import { apiFetch, ApiError } from '@/shared/api/client'
import type { AuthResponse } from '../model/types'

export async function getMe(): Promise<AuthResponse | null> {
  try {
    return await apiFetch<AuthResponse>('/api/auth/me')
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      return null
    }
    throw err
  }
}
