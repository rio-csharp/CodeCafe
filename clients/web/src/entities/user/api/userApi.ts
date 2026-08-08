import { apiFetch, ApiError } from '@/shared/api'
import type { AuthResponse } from '../model/types'

export async function getMe(signal?: AbortSignal): Promise<AuthResponse | null> {
  try {
    return await apiFetch<AuthResponse>('/api/auth/me', { signal })
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      return null
    }
    throw err
  }
}
