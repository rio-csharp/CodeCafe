import { apiFetch, clearCsrfToken } from '@/shared/api/client'
import type { AuthResponse, LoginRequest, RegisterRequest } from '@/entities/user'
export { getMe } from '@/entities/user'

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
