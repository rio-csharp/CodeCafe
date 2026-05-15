const apiBaseUrl =
  window.__CODECAFE_CONFIG__?.apiBaseUrl ??
  import.meta.env.VITE_API_BASE_URL ??
  'http://localhost:5000'

export async function apiFetch(path: string, accept = 'application/json', init?: RequestInit): Promise<Response> {
  return fetch(`${apiBaseUrl}${path}`, {
    ...init,
    credentials: init?.credentials ?? 'include',
    headers: {
      Accept: accept,
      ...init?.headers,
    },
  })
}

export async function apiJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await apiFetch(path, 'application/json', init)

  if (!response.ok) {
    throw new Error(`API request failed with status ${response.status}`)
  }

  return response.json() as Promise<T>
}

export async function apiSend<T>(path: string, method: string, body: unknown): Promise<T> {
  return apiJson<T>(path, {
    body: JSON.stringify(body),
    headers: {
      'Content-Type': 'application/json',
    },
    method,
  })
}

export async function apiDelete(path: string): Promise<void> {
  const response = await apiFetch(path, 'application/json', {
    method: 'DELETE',
  })

  if (!response.ok) {
    throw new Error(`API request failed with status ${response.status}`)
  }
}

export async function checkBackendHealth(): Promise<boolean> {
  const response = await apiFetch('/health', 'text/plain')

  return response.ok
}
