import { apiFetch, setSession } from '@/shared/api'
import type { AuthSessionDto } from '@/shared/api'

export interface LoginPayload {
  email: string
  password: string
}

export interface RegisterPayload {
  email: string
  password: string
  displayName: string
}

const LOGIN_PATH = '/api/auth/login'
const REGISTER_PATH = '/api/auth/register'

/**
 * Both calls persist the session (tokens included) and hand back the issued
 * session, so the caller can flip the UI without another round trip.
 */
export async function login(payload: LoginPayload): Promise<AuthSessionDto> {
  const session = await postSession(LOGIN_PATH, {
    email: payload.email.trim(),
    password: payload.password,
  })
  setSession(session)
  return session
}

export async function register(payload: RegisterPayload): Promise<AuthSessionDto> {
  const session = await postSession(REGISTER_PATH, {
    email: payload.email.trim(),
    password: payload.password,
    displayName: payload.displayName.trim(),
  })
  setSession(session)
  return session
}

function postSession(path: string, body: unknown): Promise<AuthSessionDto> {
  return apiFetch<AuthSessionDto>(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
