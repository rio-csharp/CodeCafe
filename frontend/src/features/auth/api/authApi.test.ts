import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { login, register, logout, getMe } from './authApi'
import { clearCsrfToken } from '../../../lib/apiClient'

describe('authApi', () => {
  beforeEach(() => {
    clearCsrfToken()
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('login', () => {
    it('sends POST to /api/auth/login with credentials and body', async () => {
      const mockFetch = vi.fn()
        .mockResolvedValueOnce({
          ok: true,
          json: vi.fn().mockResolvedValue({ token: 'csrf-abc' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          text: vi
            .fn()
            .mockResolvedValue(
              JSON.stringify({
                user: {
                  id: '1',
                  email: 'test@test.com',
                  displayName: 'Test',
                },
              }),
            ),
        })

      globalThis.fetch = mockFetch

      const result = await login({
        email: 'test@test.com',
        password: 'password123',
      })

      expect(result.user.email).toBe('test@test.com')

      const [, loginOptions] = mockFetch.mock.calls[1]
      expect(loginOptions.method).toBe('POST')
      expect(loginOptions.credentials).toBe('include')
      expect(JSON.parse(loginOptions.body)).toEqual({
        email: 'test@test.com',
        password: 'password123',
      })
    })
  })

  describe('register', () => {
    it('sends POST to /api/auth/register with credentials and body', async () => {
      const mockFetch = vi.fn()
        .mockResolvedValueOnce({
          ok: true,
          json: vi.fn().mockResolvedValue({ token: 'csrf-abc' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          text: vi
            .fn()
            .mockResolvedValue(
              JSON.stringify({
                user: {
                  id: '2',
                  email: 'new@test.com',
                  displayName: 'New',
                },
              }),
            ),
        })

      globalThis.fetch = mockFetch

      const result = await register({
        email: 'new@test.com',
        password: 'password123',
        displayName: 'New',
      })

      expect(result.user.displayName).toBe('New')

      const [, registerOptions] = mockFetch.mock.calls[1]
      expect(registerOptions.method).toBe('POST')
      expect(JSON.parse(registerOptions.body)).toEqual({
        email: 'new@test.com',
        password: 'password123',
        displayName: 'New',
      })
    })
  })

  describe('logout', () => {
    it('sends POST to /api/auth/logout with credentials', async () => {
      const mockFetch = vi.fn()
        .mockResolvedValueOnce({
          ok: true,
          json: vi.fn().mockResolvedValue({ token: 'csrf-abc' }),
        })
        .mockResolvedValueOnce({
          ok: true,
          text: vi.fn().mockResolvedValue(''),
        })

      globalThis.fetch = mockFetch

      await logout()

      const [, logoutOptions] = mockFetch.mock.calls[1]
      expect(logoutOptions.method).toBe('POST')
      expect(logoutOptions.credentials).toBe('include')
    })
  })

  describe('getMe', () => {
    it('returns user data on 200', async () => {
      globalThis.fetch = vi.fn().mockResolvedValue({
        ok: true,
        json: vi
          .fn()
          .mockResolvedValue({
            user: { id: '1', email: 'a@b.com', displayName: 'A' },
          }),
      })

      const result = await getMe()
      expect(result?.user.displayName).toBe('A')
    })

    it('returns null on 401', async () => {
      globalThis.fetch = vi.fn().mockResolvedValue({
        status: 401,
        ok: false,
      })

      const result = await getMe()
      expect(result).toBeNull()
    })

    it('throws on other errors', async () => {
      globalThis.fetch = vi.fn().mockResolvedValue({
        status: 500,
        ok: false,
        json: vi.fn().mockResolvedValue({ message: 'Server error' }),
      })

      await expect(getMe()).rejects.toThrow('Server error')
    })
  })
})
