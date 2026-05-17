export const API_BASE_URL =
  window.__CODECAFE_CONFIG__?.apiBaseUrl ??
  import.meta.env.VITE_API_BASE_URL ??
  ''

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
    this.name = 'ApiError'
  }
}

let csrfToken: string | null = null

export async function fetchCsrfToken(): Promise<string> {
  if (csrfToken) return csrfToken
  const response = await fetch(`${API_BASE_URL}/api/auth/csrf`, {
    method: 'GET',
    credentials: 'include',
  })
  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || 'Failed to fetch CSRF token')
  }
  const data: { token: string } = await response.json()
  csrfToken = data.token
  return data.token
}

export function clearCsrfToken(): void {
  csrfToken = null
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const method = options.method?.toUpperCase() || 'GET'
  const isMutating = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)

  const headers = new Headers(options.headers)
  if (options.body && typeof options.body === 'string') {
    headers.set('Content-Type', 'application/json')
  }

  if (isMutating) {
    const token: string = csrfToken ?? await fetchCsrfToken()
    headers.set('X-CSRF-TOKEN', token)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    credentials: 'include',
    headers,
  })

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}))
    throw new ApiError(
      response.status,
      errorData.message || `Request failed with status ${response.status}`,
    )
  }

  const text = await response.text()
  return (text ? JSON.parse(text) : undefined) as T
}
