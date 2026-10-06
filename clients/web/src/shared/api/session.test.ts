import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from './errors'
import {
  clearSession,
  getAccessToken,
  getRefreshToken,
  onSessionCleared,
  refreshSession,
  setSession,
  setSessionClock,
  REFRESH_STORAGE_KEY,
} from './session'
import type { AuthSessionDto } from './types'

const USER = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

function authSession(overrides: Partial<AuthSessionDto> = {}): AuthSessionDto {
  return {
    user: USER,
    accessToken: 'access-1',
    accessTokenExpiresAtUtc: '2026-01-01T00:15:00.000Z',
    refreshToken: 'refresh-1',
    ...overrides,
  }
}

function jsonResponse(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  })
}

function success(body: unknown): Response {
  return jsonResponse({ value: body, error: null, isSuccess: true }, 200)
}

function failure(code: string, kind: string, status: number): Response {
  return jsonResponse({ value: null, error: { code, message: code, kind }, isSuccess: false }, status)
}

/** A fetch stub whose calls are inspectable, typed like the real `fetch`. */
function stubFetch(handler: (url: string, init?: RequestInit) => Promise<Response>) {
  const mock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => handler(String(input), init))
  vi.stubGlobal('fetch', mock)
  return mock
}

beforeEach(() => {
  window.localStorage.clear()
  clearSession()
  setSessionClock(null)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('session storage', () => {
  it('keeps the access token in memory and the refresh token in localStorage', () => {
    setSession(authSession())

    expect(getAccessToken()).toBe('access-1')
    expect(getRefreshToken()).toBe('refresh-1')
    expect(window.localStorage.getItem(REFRESH_STORAGE_KEY)).toBe('refresh-1')
  })

  it('clears both tokens and tells every listener', () => {
    setSession(authSession())
    const listener = vi.fn()
    const unsubscribe = onSessionCleared(listener)

    clearSession()

    expect(getAccessToken()).toBeNull()
    expect(getRefreshToken()).toBeNull()
    expect(window.localStorage.getItem(REFRESH_STORAGE_KEY)).toBeNull()
    expect(listener).toHaveBeenCalledTimes(1)

    unsubscribe()
    clearSession()
    expect(listener).toHaveBeenCalledTimes(1)
  })
})

describe('refreshSession', () => {
  it('sends one request for concurrent callers', async () => {
    let release: () => void = () => undefined
    const gate = new Promise<void>((resolve) => {
      release = () => {
        resolve()
      }
    })
    const fetchMock = stubFetch(async () => {
      await gate
      return success(authSession({ refreshToken: 'refresh-2' }))
    })
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')

    const first = refreshSession()
    const second = refreshSession()
    release()
    const [firstSession, secondSession] = await Promise.all([first, second])

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(firstSession).toBe(secondSession)
  })

  it('persists the rotated refresh token', async () => {
    stubFetch(async () => success(authSession({ accessToken: 'access-2', refreshToken: 'refresh-2' })))
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')

    await refreshSession()

    expect(getAccessToken()).toBe('access-2')
    expect(getRefreshToken()).toBe('refresh-2')
    expect(window.localStorage.getItem(REFRESH_STORAGE_KEY)).toBe('refresh-2')
  })

  it('posts the stored refresh token as the credential', async () => {
    const fetchMock = stubFetch(async () => success(authSession()))
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')

    await refreshSession()

    const [url, init] = fetchMock.mock.calls[0] ?? []
    expect(url).toBe('/api/auth/refresh')
    expect(JSON.parse(String(init?.body))).toEqual({ refreshToken: 'refresh-1' })
  })

  it('clears the session and rejects every waiter when the token is refused', async () => {
    stubFetch(async () => failure('invalid_refresh_token', 'Unauthorized', 401))
    setSession(authSession())
    const listener = vi.fn()
    onSessionCleared(listener)

    const results = await Promise.allSettled([refreshSession(), refreshSession()])

    expect(results.map((result) => result.status)).toEqual(['rejected', 'rejected'])
    expect(results[0]).toMatchObject({ reason: expect.any(ApiError) })
    expect(getAccessToken()).toBeNull()
    expect(getRefreshToken()).toBeNull()
    expect(listener).toHaveBeenCalledTimes(1)
  })

  it('rejects without a request when there is no refresh token', async () => {
    const fetchMock = stubFetch(async () => success(authSession()))

    const error = await refreshSession().catch((cause: unknown) => cause)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 401, code: 'invalid_refresh_token' })
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('retries on the next call after a failed refresh', async () => {
    const fetchMock = stubFetch(async () => failure('invalid_refresh_token', 'Unauthorized', 401))
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')

    await expect(refreshSession()).rejects.toBeInstanceOf(ApiError)

    // Rotation is over — a later call must not be stuck behind the dead promise.
    fetchMock.mockResolvedValue(success(authSession({ refreshToken: 'refresh-2' })))
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')

    await expect(refreshSession()).resolves.toMatchObject({ accessToken: 'access-1' })
  })
})
