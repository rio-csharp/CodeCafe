import { ApiError, kindFromStatus } from './errors'
import type { ResultEnvelope } from './types'

const DEFAULT_TIMEOUT_MS = 30_000

export interface JsonRequestInit extends Omit<RequestInit, 'signal'> {
  signal?: AbortSignal | null
}

/**
 * The raw JSON transport: sends the request and unwraps the `Result` envelope.
 * It knows nothing about auth — `client.ts` sits on top of it, and `session.ts`
 * calls it directly so a refresh can never recurse into the 401 retry logic.
 */
export async function requestJson<T>(path: string, init: JsonRequestInit = {}): Promise<T> {
  const { signal, headers, ...rest } = init

  // ASP.NET refuses a body it cannot identify (415); a JSON gateway declares
  // JSON the moment a body is present. Callers may still override.
  const withContentType =
    rest.body !== undefined ? mergeHeader(headers, 'Content-Type', 'application/json') : headers

  const response = await fetch(path, {
    ...rest,
    signal: withTimeout(signal),
    headers: mergeHeader(withContentType, 'Accept', 'application/json'),
  })

  return unwrapResponse<T>(response)
}

/** Adds a header only when the caller has not set it, so callers still win. */
export function mergeHeader(
  headers: HeadersInit | undefined,
  name: string,
  value: string,
): HeadersInit {
  const merged = new Headers(headers)
  if (!merged.has(name)) {
    merged.set(name, value)
  }
  return merged
}

function withTimeout(callerSignal: AbortSignal | null | undefined): AbortSignal {
  const timeout = AbortSignal.timeout(DEFAULT_TIMEOUT_MS)
  return callerSignal ? AbortSignal.any([callerSignal, timeout]) : timeout
}

async function unwrapResponse<T>(response: Response): Promise<T> {
  const raw = await response.text()

  if (raw.trim().length === 0) {
    if (response.ok) {
      return undefined as T
    }
    throw new ApiError({
      status: response.status,
      code: 'client.empty_response',
      kind: kindFromStatus(response.status),
      message: `Request failed with status ${response.status}.`,
    })
  }

  let payload: unknown
  try {
    payload = JSON.parse(raw)
  } catch (cause) {
    throw new ApiError({
      status: response.status,
      code: 'client.malformed_response',
      kind: kindFromStatus(response.status),
      message: 'The server returned a body that is not valid JSON.',
      cause,
    })
  }

  const envelope = asEnvelope(payload)

  if (envelope?.error) {
    const { code, message, kind } = envelope.error
    throw new ApiError({ status: response.status, code, kind, message })
  }

  if (envelope && !envelope.isSuccess) {
    throw new ApiError({
      status: response.status,
      code: 'client.unknown_error',
      kind: kindFromStatus(response.status),
      message: 'The request did not succeed.',
    })
  }

  if (!response.ok) {
    throw new ApiError({
      status: response.status,
      code: 'client.request_failed',
      kind: kindFromStatus(response.status),
      message: `Request failed with status ${response.status}.`,
    })
  }

  return (envelope ? envelope.value : payload) as T
}

function asEnvelope(payload: unknown): ResultEnvelope<unknown> | null {
  if (typeof payload !== 'object' || payload === null) {
    return null
  }
  const candidate = payload as Partial<ResultEnvelope<unknown>>
  // The escape hatches (markdown export, SSE) are not envelopes — leave them alone.
  return 'isSuccess' in candidate || 'error' in candidate
    ? (candidate as ResultEnvelope<unknown>)
    : null
}
