import { API_BASE_URL, apiFetch, clearCsrfToken } from '../../../lib/apiClient'
import type { AuthResponse, LoginRequest, RegisterRequest } from '../types'

export async function login(data: LoginRequest): Promise<AuthResponse> {
  const result = await apiFetch<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(data),
  })
  clearCsrfToken()
  return result
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const result = await apiFetch<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: JSON.stringify(data),
  })
  clearCsrfToken()
  return result
}

export async function logout(): Promise<void> {
  await apiFetch<void>('/api/auth/logout', {
    method: 'POST',
  })
  clearCsrfToken()
}

export async function getMe(): Promise<AuthResponse | null> {
  const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
    method: 'GET',
    credentials: 'include',
  })

  if (response.status === 401) {
    return null
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({}))
    throw new Error(error.message || 'Failed to fetch user')
  }

  return response.json()
}
