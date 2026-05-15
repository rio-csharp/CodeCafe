import { apiFetch, apiJson } from '../../lib/apiClient'

export type AuthSession = {
  isAuthenticated: boolean
  username: string | null
}

export class AuthenticationError extends Error {
  constructor(message = 'Invalid username or password.') {
    super(message)
    this.name = 'AuthenticationError'
  }
}

export class AuthenticationRateLimitError extends Error {
  constructor(message = 'Too many sign-in attempts. Please wait and try again later.') {
    super(message)
    this.name = 'AuthenticationRateLimitError'
  }
}

export class AuthenticationUnavailableError extends Error {
  constructor(message = 'Authentication service is unavailable right now.') {
    super(message)
    this.name = 'AuthenticationUnavailableError'
  }
}

export function getSession(): Promise<AuthSession> {
  return apiJson<AuthSession>('/api/auth/session')
}

export async function login(username: string, password: string): Promise<AuthSession> {
  const response = await apiFetch('/api/auth/login', 'application/json', {
    body: JSON.stringify({ password, username }),
    headers: {
      'Content-Type': 'application/json',
    },
    method: 'POST',
  })

  if (response.status === 401) {
    throw new AuthenticationError()
  }

  if (response.status === 429) {
    throw new AuthenticationRateLimitError()
  }

  if (!response.ok) {
    throw new AuthenticationUnavailableError()
  }

  return response.json() as Promise<AuthSession>
}

export async function logout(): Promise<void> {
  const response = await apiFetch('/api/auth/logout', 'application/json', {
    method: 'POST',
  })

  if (response.status === 401) {
    return
  }

  if (!response.ok) {
    throw new AuthenticationUnavailableError('Unable to sign out right now.')
  }
}
