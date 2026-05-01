const apiBaseUrl =
  window.__CODECAFE_CONFIG__?.apiBaseUrl ??
  import.meta.env.VITE_API_BASE_URL ??
  'http://localhost:5000'

async function apiFetch(path: string, accept: string): Promise<Response> {
  return fetch(`${apiBaseUrl}${path}`, {
    headers: {
      Accept: accept,
    },
  })
}

export async function apiGet<TResponse>(path: string): Promise<TResponse> {
  const response = await apiFetch(path, 'application/json')

  if (!response.ok) {
    throw new Error(`API request failed with status ${response.status}`)
  }

  return response.json() as Promise<TResponse>
}

export async function checkBackendHealth(): Promise<boolean> {
  const response = await apiFetch('/health', 'text/plain')

  return response.ok
}
