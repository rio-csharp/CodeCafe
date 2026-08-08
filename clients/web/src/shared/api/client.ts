import { API_BASE_URL } from '@/shared/config'
import i18n from '@/shared/lib/i18n'
import { ApiError } from './ApiError'

export { API_BASE_URL }
export { ApiError }

let csrfToken: string | null = null
let csrfTokenInFlight: Promise<string> | null = null

// Requests abort when this timeout elapses or when the caller's signal
// aborts, whichever comes first — a hung request must not pend forever.
const DEFAULT_TIMEOUT_MS = 30_000

function withDefaultTimeout(signal: AbortSignal | null | undefined): AbortSignal {
  const timeoutSignal = AbortSignal.timeout(DEFAULT_TIMEOUT_MS)
  return signal ? AbortSignal.any([signal, timeoutSignal]) : timeoutSignal
}

export async function fetchCsrfToken(): Promise<string> {
  if (csrfToken) return csrfToken
  if (csrfTokenInFlight) return csrfTokenInFlight

  csrfTokenInFlight = (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/csrf`, {
        method: 'GET',
        credentials: 'include',
        signal: AbortSignal.timeout(DEFAULT_TIMEOUT_MS),
      })
      if (!response.ok) {
        const error = await response.json().catch(() => ({}))
        throw new Error(error.detail || error.message || i18n.t('errors.csrfFailed'))
      }
      const data: { token?: unknown } = await response.json()
      // A 200 without a usable token would otherwise send a literal
      // "undefined" X-CSRF-TOKEN header on every mutating request.
      if (typeof data.token !== 'string' || !data.token) {
        throw new Error(i18n.t('errors.csrfFailed'))
      }
      csrfToken = data.token
      return data.token
    } finally {
      csrfTokenInFlight = null
    }
  })()

  return csrfTokenInFlight
}

export function clearCsrfToken(): void {
  csrfToken = null
}

async function performFetch<T>(
  path: string,
  options: RequestInit,
  isMutating: boolean,
): Promise<T> {
  const headers = new Headers(options.headers)
  if (options.body && typeof options.body === 'string') {
    headers.set('Content-Type', 'application/json')
  }

  if (isMutating) {
    const token = csrfToken ?? (await fetchCsrfToken())
    headers.set('X-CSRF-TOKEN', token)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    credentials: 'include',
    headers,
    signal: withDefaultTimeout(options.signal),
  })

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}))
    const code: string | undefined = errorData.code ?? errorData.title
    const message: string =
      errorData.detail ??
      errorData.message ??
      `Request failed with status ${response.status}`
    throw new ApiError(response.status, message, code)
  }

  const text = await response.text()
  return (text.trim() ? JSON.parse(text) : undefined) as T
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const method = options.method?.toUpperCase() || 'GET'
  const isMutating = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)

  try {
    return await performFetch<T>(path, options, isMutating)
  } catch (err) {
    if (
      isMutating &&
      err instanceof ApiError &&
      err.status === 400 &&
      err.code === 'invalid_csrf_token'
    ) {
      clearCsrfToken()
      return await performFetch<T>(path, options, isMutating)
    }
    throw err
  }
}
