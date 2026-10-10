import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  changePassword,
  createPersonalAccessToken,
  listPersonalAccessTokens,
  revokePersonalAccessToken,
  updateProfile,
} from './accountApi'

function ok(value: unknown) {
  return new Response(JSON.stringify({ value, error: null, isSuccess: true }), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  })
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(async () => ok(null)))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('updateProfile', () => {
  it('patches the display name and returns the user', async () => {
    const user = { id: 'u1', email: 'ada@example.com', displayName: 'Ada L.' }
    vi.mocked(fetch).mockResolvedValueOnce(ok(user))

    const result = await updateProfile('Ada L.')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/auth/me',
      expect.objectContaining({ method: 'PATCH', body: JSON.stringify({ displayName: 'Ada L.' }) }),
    )
    expect(result).toEqual(user)
  })
})

describe('changePassword', () => {
  it('posts both passwords', async () => {
    await changePassword('old-password', 'new-password-123')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/auth/change-password',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ currentPassword: 'old-password', newPassword: 'new-password-123' }),
      }),
    )
  })
})

describe('personal access tokens', () => {
  it('lists tokens', async () => {
    const tokens = [
      {
        id: 't1',
        name: 'CLI',
        createdAtUtc: '2026-01-01T00:00:00Z',
        expiresAtUtc: '2026-04-01T00:00:00Z',
        revokedAtUtc: null,
      },
    ]
    vi.mocked(fetch).mockResolvedValueOnce(ok(tokens))

    const result = await listPersonalAccessTokens()

    expect(vi.mocked(fetch)).toHaveBeenCalledWith('/api/auth/tokens', expect.anything())
    expect(result).toEqual(tokens)
  })

  it('creates a token, passing null when no expiry is chosen', async () => {
    await createPersonalAccessToken({ name: 'CLI', expiresInDays: null })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/auth/tokens',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ name: 'CLI', expiresInDays: null }),
      }),
    )
  })

  it('creates a token with an expiry in days', async () => {
    await createPersonalAccessToken({ name: 'CLI', expiresInDays: 30 })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/auth/tokens',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ name: 'CLI', expiresInDays: 30 }),
      }),
    )
  })

  it('revokes a token by id', async () => {
    await revokePersonalAccessToken('t1')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/auth/tokens/t1',
      expect.objectContaining({ method: 'DELETE' }),
    )
  })
})
