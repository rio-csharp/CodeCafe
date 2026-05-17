import { apiFetch, ApiError, clearCsrfToken } from '../../../lib/apiClient'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types'

export async function login(data: LoginRequest): Promise<AuthResponse> {
  const response = await apiFetch<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(data),
  })
  clearCsrfToken()
  return response
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const response = await apiFetch<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  })
  clearCsrfToken()
  return response
}

export async function logout(): Promise<void> {
  await apiFetch<void>('/api/auth/logout', {
    method: 'POST',
  })
  clearCsrfToken()
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
