import { apiFetch, ApiError } from '../../../lib/apiClient'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types'

export async function login(data: LoginRequest): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  return apiFetch<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}

export async function logout(): Promise<void> {
  await apiFetch<void>('/api/auth/logout', {
    method: 'POST',
  })
}

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
