import { API_BASE_URL } from '../apiClient'

export type HealthStatus = 'ok' | 'error' | 'offline'

export interface HealthResult {
  status: HealthStatus
}

export async function fetchHealth(): Promise<HealthResult> {
  try {
    const response = await fetch(`${API_BASE_URL}/health/live`, {
      method: 'GET',
    })

    if (!response.ok) {
      return { status: 'error' }
    }

    return { status: 'ok' }
  } catch {
    // Network error, CORS, or certificate issue
    return { status: 'offline' }
  }
}
