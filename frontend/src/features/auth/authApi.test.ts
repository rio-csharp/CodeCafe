import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  AuthenticationError,
  AuthenticationRateLimitError,
  AuthenticationUnavailableError,
  getSession,
  login,
  logout,
} from './authApi'

describe('authApi', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('gets the current session with credentialed requests', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      Response.json({ isAuthenticated: true, username: 'admin' }),
    )

    await expect(getSession()).resolves.toEqual({
      isAuthenticated: true,
      username: 'admin',
    })
    expect(fetchSpy).toHaveBeenCalledWith(
      'http://localhost:5000/api/auth/session',
      expect.objectContaining({
        credentials: 'include',
        headers: {
          Accept: 'application/json',
        },
      }),
    )
  })

  it('returns the authenticated session after a successful login', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      Response.json({ isAuthenticated: true, username: 'admin' }),
    )

    await expect(login('admin', 'secret')).resolves.toEqual({
      isAuthenticated: true,
      username: 'admin',
    })
  })

  it('maps invalid credentials to AuthenticationError', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 401 }))

    await expect(login('admin', 'bad')).rejects.toBeInstanceOf(AuthenticationError)
  })

  it('maps throttled requests to AuthenticationRateLimitError', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 429 }))

    await expect(login('admin', 'bad')).rejects.toBeInstanceOf(AuthenticationRateLimitError)
  })

  it('maps backend failures to AuthenticationUnavailableError during login', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 503 }))

    await expect(login('admin', 'secret')).rejects.toBeInstanceOf(AuthenticationUnavailableError)
  })

  it('treats 401 logout as already signed out', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 401 }))

    await expect(logout()).resolves.toBeUndefined()
  })

  it('throws AuthenticationUnavailableError when logout fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 503 }))

    await expect(logout()).rejects.toBeInstanceOf(AuthenticationUnavailableError)
  })
})
