export const API_BASE_URL =
  window.__CODECAFE_CONFIG__?.apiBaseUrl ??
  import.meta.env.VITE_API_BASE_URL ??
  ''

export class ApiError extends Error {
  status: number
  code?: string
  constructor(status: number, message: string, code?: string) {
    super(message)
    this.status = status
    this.code = code
    this.name = 'ApiError'
  }
}

let csrfToken: string | null = null
let csrfTokenInFlight: Promise<string> | null = null

export async function fetchCsrfToken(): Promise<string> {
  if (csrfToken) return csrfToken
  if (csrfTokenInFlight) return csrfTokenInFlight

  csrfTokenInFlight = (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/csrf`, {
        method: 'GET',
        credentials: 'include',
      })
      if (!response.ok) {
        const error = await response.json().catch(() => ({}))
        throw new Error(error.detail || error.message || 'Failed to fetch CSRF token')
      }
      const data: { token: string } = await response.json()
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
  return (text ? JSON.parse(text) : undefined) as T
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
