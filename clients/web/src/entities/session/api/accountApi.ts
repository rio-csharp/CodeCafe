import { apiFetch } from '@/shared/api'
import type { AuthUserDto } from '@/shared/api'

export interface PersonalAccessToken {
  id: string
  name: string
  createdAtUtc: string
  expiresAtUtc: string
  revokedAtUtc: string | null
}

/**
 * The create response — the only payload that ever carries the raw token;
 * afterwards the server stores just its hash, so the UI must show it once.
 */
export interface CreatedPersonalAccessToken {
  id: string
  name: string
  createdAtUtc: string
  expiresAtUtc: string
  token: string
}

export interface CreatePersonalAccessTokenPayload {
  name: string
  /** Null lets the server apply its default lifetime. */
  expiresInDays: number | null
}

export const sessionKeys = {
  tokens: ['session', 'tokens'] as const,
}

export function updateProfile(displayName: string): Promise<AuthUserDto> {
  return apiFetch<AuthUserDto>('/api/auth/me', {
    method: 'PATCH',
    body: JSON.stringify({ displayName }),
  })
}

export function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  return apiFetch<void>('/api/auth/change-password', {
    method: 'POST',
    body: JSON.stringify({ currentPassword, newPassword }),
  })
}

export function listPersonalAccessTokens(signal?: AbortSignal): Promise<PersonalAccessToken[]> {
  return apiFetch<PersonalAccessToken[]>('/api/auth/tokens', { signal })
}

export function createPersonalAccessToken(
  payload: CreatePersonalAccessTokenPayload,
): Promise<CreatedPersonalAccessToken> {
  return apiFetch<CreatedPersonalAccessToken>('/api/auth/tokens', {
    method: 'POST',
    body: JSON.stringify({ name: payload.name, expiresInDays: payload.expiresInDays }),
  })
}

export function revokePersonalAccessToken(id: string): Promise<void> {
  return apiFetch<void>(`/api/auth/tokens/${encodeURIComponent(id)}`, { method: 'DELETE' })
}
