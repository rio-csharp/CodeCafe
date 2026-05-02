const apiBaseUrl =
  window.__CODECAFE_CONFIG__?.apiBaseUrl ??
  import.meta.env.VITE_API_BASE_URL ??
  'http://localhost:5000'

export async function apiFetch(path: string, accept: string): Promise<Response> {
  return fetch(`${apiBaseUrl}${path}`, {
    headers: {
      Accept: accept,
    },
  })
}

export async function checkBackendHealth(): Promise<boolean> {
  const response = await apiFetch('/health', 'text/plain')

  return response.ok
}
