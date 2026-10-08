import { getAccessToken } from '@/shared/api'

/** One frame of the AI chat stream, discriminated by `kind`. */
export type AiChatEvent =
  | { kind: 'text'; text: string }
  | { kind: 'tool-call'; tool: string; callId: string }
  | { kind: 'tool-result'; callId: string }
  | { kind: 'error'; code: string; message: string }
  | { kind: 'done' }

export interface AiChatWireMessage {
  role: 'User' | 'Assistant'
  content: string
}

/**
 * A failure BEFORE the stream starts (401/404/429…): the response is a normal
 * JSON error envelope, not SSE. Once the stream is flowing, failures arrive
 * as `error` events instead.
 */
export class AiChatHttpError extends Error {
  readonly status: number
  readonly code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = 'AiChatHttpError'
    this.status = status
    this.code = code
  }
}

export interface StreamChatParams {
  slug: string
  messages: readonly AiChatWireMessage[]
  signal: AbortSignal
  onEvent: (event: AiChatEvent) => void
}

/**
 * POSTs the conversation and streams the reply as Server-Sent Events.
 * EventSource cannot POST, so frames are parsed by hand off the body stream.
 */
export async function streamChat({
  slug,
  messages,
  signal,
  onEvent,
}: StreamChatParams): Promise<void> {
  const token = getAccessToken()
  const response = await fetch(`/api/notebooks/${encodeURIComponent(slug)}/ai/chat`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token !== null ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify({ messages }),
    signal,
  })

  if (!response.ok || response.body === null) {
    throw await toHttpError(response)
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  for (;;) {
    const { done, value } = await reader.read()
    if (done) {
      break
    }
    buffer += decoder.decode(value, { stream: true })
    // Frames are separated by a blank line; the last chunk may end mid-frame.
    let separator = buffer.indexOf('\n\n')
    while (separator !== -1) {
      const frame = buffer.slice(0, separator)
      buffer = buffer.slice(separator + 2)
      const event = parseFrame(frame)
      if (event !== null) {
        onEvent(event)
      }
      separator = buffer.indexOf('\n\n')
    }
  }
}

async function toHttpError(response: Response): Promise<AiChatHttpError> {
  try {
    const envelope = (await response.json()) as { error?: { code?: string; message?: string } }
    return new AiChatHttpError(
      response.status,
      envelope.error?.code ?? 'unknown',
      envelope.error?.message ?? `Request failed (${response.status})`,
    )
  } catch {
    return new AiChatHttpError(response.status, 'unknown', `Request failed (${response.status})`)
  }
}

/** One SSE frame: `event: <kind>` plus one or more `data:` lines of JSON. */
function parseFrame(frame: string): AiChatEvent | null {
  let name = ''
  const dataLines: string[] = []
  for (const line of frame.split('\n')) {
    if (line.startsWith('event:')) {
      name = line.slice(6).trim()
    } else if (line.startsWith('data:')) {
      dataLines.push(line.slice(5).trimStart())
    }
  }
  if (name === '' || dataLines.length === 0) {
    return null
  }
  try {
    const payload = JSON.parse(dataLines.join('\n')) as Record<string, unknown>
    switch (name) {
      case 'text':
        return { kind: 'text', text: String(payload.text ?? '') }
      case 'tool-call':
        return { kind: 'tool-call', tool: String(payload.tool ?? ''), callId: String(payload.callId ?? '') }
      case 'tool-result':
        return { kind: 'tool-result', callId: String(payload.callId ?? '') }
      case 'error':
        return {
          kind: 'error',
          code: String(payload.code ?? 'unknown'),
          message: String(payload.message ?? ''),
        }
      case 'done':
        return { kind: 'done' }
      default:
        return null
    }
  } catch {
    // A malformed frame is skipped, not fatal: the rest of the stream is fine.
    return null
  }
}
