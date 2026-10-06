import { waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { clearSession, setSession, REFRESH_STORAGE_KEY } from '@/shared/api'
import type { AuthSessionDto } from '@/shared/api'
import { useSessionStore } from './sessionStore'
import { bootstrapSession } from './session'

const USER = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const SESSION: AuthSessionDto = {
  user: USER,
  accessToken: 'access-1',
  accessTokenExpiresAtUtc: '2099-01-01T00:00:00.000Z',
  refreshToken: 'refresh-2',
}

function jsonResponse(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  })
}

beforeEach(() => {
  window.localStorage.clear()
  clearSession()
  useSessionStore.setState({ status: 'unknown', user: null })
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('bootstrapSession', () => {
  it('goes anonymous straight away without a refresh token', () => {
    const unsubscribe = bootstrapSession()

    expect(useSessionStore.getState().status).toBe('anonymous')
    unsubscribe()
  })

  it('resumes the session when a stored refresh token still works', async () => {
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        jsonResponse({ value: SESSION, error: null, isSuccess: true }, 200),
      ),
    )

    const unsubscribe = bootstrapSession()

    await waitFor(() => {
      expect(useSessionStore.getState().status).toBe('authenticated')
    })
    expect(useSessionStore.getState().user).toEqual(USER)
    unsubscribe()
  })

  it('goes anonymous when the stored refresh token is refused', async () => {
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        jsonResponse(
          {
            value: null,
            error: {
              code: 'invalid_refresh_token',
              message: 'expired',
              kind: 'Unauthorized',
            },
            isSuccess: false,
          },
          401,
        ),
      ),
    )

    const unsubscribe = bootstrapSession()

    await waitFor(() => {
      expect(useSessionStore.getState().status).toBe('anonymous')
    })
    expect(useSessionStore.getState().user).toBeNull()
    unsubscribe()
  })

  it('follows the session down whenever it is cleared', () => {
    const unsubscribe = bootstrapSession()
    setSession(SESSION)
    useSessionStore.getState().setAuthenticated(USER)

    clearSession()

    expect(useSessionStore.getState()).toMatchObject({ status: 'anonymous', user: null })
    unsubscribe()
  })
})
