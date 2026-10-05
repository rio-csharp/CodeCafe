import type { ErrorKind, ResultEnvelope } from './types'

const DEFAULT_TIMEOUT_MS = 30_000

const STATUS_TO_KIND: Readonly<Record<number, ErrorKind>> = {
  400: 'Validation',
  401: 'Unauthorized',
  403: 'Forbidden',
  404: 'NotFound',
  409: 'Conflict',
  429: 'RateLimited',
}

/** Everything above `shared/` sees when a request fails — never the envelope itself. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly kind: ErrorKind

  constructor(args: {
    status: number
    code: string
    kind: ErrorKind
    message: string
    cause?: unknown
  }) {
    super(args.message, args.cause === undefined ? undefined : { cause: args.cause })
    this.name = 'ApiError'
    this.status = args.status
    this.code = args.code
    this.kind = args.kind
  }
}

export interface ApiFetchInit extends Omit<RequestInit, 'signal'> {
  signal?: AbortSignal | null
}

/**
 * JSON-only gateway to the API. Success returns the unwrapped `value`; anything
 * else throws {@link ApiError}. A 204 / empty body resolves to `undefined`.
 */
export async function apiFetch<T>(path: string, init: ApiFetchInit = {}): Promise<T> {
  const { signal, headers, ...rest } = init

  const response = await fetch(path, {
    ...rest,
    signal: withTimeout(signal),
    headers: { Accept: 'application/json', ...headers },
  })

  return unwrapResponse<T>(response)
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

function kindFromStatus(status: number): ErrorKind {
  return STATUS_TO_KIND[status] ?? 'Unexpected'
}
