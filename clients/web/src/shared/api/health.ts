import { API_BASE_URL } from './client'

export type HealthStatus = 'ok' | 'error' | 'offline'

export interface HealthResult {
  status: HealthStatus
}

export async function fetchHealth(signal?: AbortSignal): Promise<HealthResult> {
  try {
    const response = await fetch(`${API_BASE_URL}/health/live`, {
      method: 'GET',
      signal,
    })

    if (!response.ok) {
      return { status: 'error' }
    }

    return { status: 'ok' }
  } catch (error) {
    // An abort is the caller unmounting or refetching, not an unhealthy server;
    // reporting "offline" would flash a false error state in the UI.
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error
    }

    // Network error, CORS, or certificate issue
    return { status: 'offline' }
  }
}
