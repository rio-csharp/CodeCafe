import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiFetch } from './client'
import {
  clearSession,
  getAccessToken,
  getRefreshToken,
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
    // Far enough out that no test hits the proactive-refresh window by accident.
    accessTokenExpiresAtUtc: '2099-01-01T00:00:00.000Z',
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

describe('content negotiation', () => {
  it('declares application/json whenever a body rides along', async () => {
    const fetchMock = stubFetch(async () => success({ ok: true }))

    await apiFetch('/api/notebooks', { method: 'POST', body: JSON.stringify({ title: 'x' }) })

    const headers = new Headers(fetchMock.mock.calls[0][1]?.headers)
    expect(headers.get('Content-Type')).toBe('application/json')
    expect(headers.get('Accept')).toBe('application/json')
  })

  it('leaves a caller-set Content-Type alone and bodiless requests bare', async () => {
    const fetchMock = stubFetch(async () => success({ ok: true }))

    await apiFetch('/api/x', {
      method: 'POST',
      body: 'raw',
      headers: { 'Content-Type': 'text/plain' },
    })
    await apiFetch('/api/y')

    const [custom, bare] = fetchMock.mock.calls
    expect(new Headers(custom[1]?.headers).get('Content-Type')).toBe('text/plain')
    expect(new Headers(bare[1]?.headers).get('Content-Type')).toBeNull()
  })
})

function bearerOf(init: RequestInit | undefined): string | null {
  return new Headers(init?.headers).get('Authorization')
}

beforeEach(() => {
  window.localStorage.clear()
  clearSession()
  setSessionClock(null)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('apiFetch', () => {
  it('unwraps the success envelope', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        jsonResponse({ value: { items: [1, 2] }, error: null, isSuccess: true }, 200),
      ),
    )

    await expect(apiFetch<{ items: number[] }>('/api/notebooks')).resolves.toEqual({ items: [1, 2] })
  })

  it('resolves to undefined for an empty body', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(null, { status: 204 })))

    await expect(apiFetch<void>('/api/notebooks')).resolves.toBeUndefined()
  })

  it('throws ApiError with status, code and kind on a failure envelope', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        jsonResponse(
          {
            value: null,
            error: {
              code: 'notebooks.search_invalid',
              message: 'Search term is too long.',
              kind: 'Validation',
            },
            isSuccess: false,
          },
          400,
        ),
      ),
    )

    const error = await apiFetch('/api/notebooks').catch((cause: unknown) => cause)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      code: 'notebooks.search_invalid',
      kind: 'Validation',
      message: 'Search term is too long.',
    })
  })

  it('throws when an error response is not valid JSON', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => new Response('<html>boom</html>', { status: 500 })),
    )

    const error = await apiFetch('/api/notebooks').catch((cause: unknown) => cause)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 500,
      code: 'client.malformed_response',
      kind: 'Unexpected',
    })
  })
})

describe('apiFetch with a session', () => {
  it('attaches the access token as a bearer header', async () => {
    const fetchMock = stubFetch(async () => success({ id: 'u1' }))
    setSession(authSession())

    await apiFetch('/api/auth/me')

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(bearerOf(fetchMock.mock.calls[0]?.[1])).toBe('Bearer access-1')
  })

  it('refreshes and retries once after a 401', async () => {
    const fetchMock = stubFetch(async (url) => {
      if (url === '/api/auth/refresh') {
        return success(authSession({ accessToken: 'access-2', refreshToken: 'refresh-2' }))
      }
      return bearerOf(fetchMock.mock.calls.at(-1)?.[1]) === 'Bearer access-2'
        ? success({ id: 'u1' })
        : failure('invalid_access_token', 'Unauthorized', 401)
    })
    setSession(authSession())

    await expect(apiFetch('/api/auth/me')).resolves.toEqual({ id: 'u1' })

    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/auth/refresh')
    expect(bearerOf(fetchMock.mock.calls[2]?.[1])).toBe('Bearer access-2')
  })

  it('does not refresh or replay an old request after the signed-in user changes', async () => {
    let release: () => void = () => undefined
    const gate = new Promise<void>((resolve) => {
      release = resolve
    })
    const fetchMock = stubFetch(async () => {
      await gate
      return failure('invalid_access_token', 'Unauthorized', 401)
    })
    setSession(authSession())

    const pending = apiFetch('/api/notebooks/old-user', {
      method: 'POST',
      body: JSON.stringify({ title: 'Must not be replayed' }),
    })
    setSession(
      authSession({
        user: { id: 'u2', email: 'grace@example.com', displayName: 'Grace' },
        accessToken: 'access-b',
        refreshToken: 'refresh-b',
      }),
    )
    release()

    await expect(pending).rejects.toMatchObject({ name: 'AbortError' })
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/notebooks/old-user')
    expect(getAccessToken()).toBe('access-b')
    expect(getRefreshToken()).toBe('refresh-b')
  })

  it('clears the session and throws when the retry is refused too', async () => {
    stubFetch(async (url) =>
      url === '/api/auth/refresh'
        ? success(authSession({ accessToken: 'access-2', refreshToken: 'refresh-2' }))
        : failure('invalid_access_token', 'Unauthorized', 401),
    )
    setSession(authSession())

    const error = await apiFetch('/api/auth/me').catch((cause: unknown) => cause)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 401 })
    expect(getAccessToken()).toBeNull()
    expect(getRefreshToken()).toBeNull()
    expect(window.localStorage.getItem(REFRESH_STORAGE_KEY)).toBeNull()
  })

  it('never retries an anonymous request', async () => {
    const fetchMock = stubFetch(async () => failure('unauthorized', 'Unauthorized', 401))

    const error = await apiFetch('/api/auth/me').catch((cause: unknown) => cause)

    expect(error).toBeInstanceOf(ApiError)
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(bearerOf(fetchMock.mock.calls[0]?.[1])).toBeNull()
  })

  it('refreshes before sending when the token expires within the minute', async () => {
    const fetchMock = stubFetch(async () => success({ id: 'u1' }))
    setSession(authSession({ accessTokenExpiresAtUtc: '2026-01-01T00:01:00.000Z' }))
    // 45 s to live: inside the 60 s window.
    setSessionClock({ now: () => Date.parse('2026-01-01T00:00:15.000Z') })

    await apiFetch('/api/auth/me')

    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/auth/refresh')
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('does not send with a new user after an old proactive refresh finishes', async () => {
    let release: () => void = () => undefined
    const gate = new Promise<void>((resolve) => {
      release = resolve
    })
    const fetchMock = stubFetch(async (url) => {
      expect(url).toBe('/api/auth/refresh')
      await gate
      return success(authSession({ accessToken: 'late-access', refreshToken: 'late-refresh' }))
    })
    setSession(authSession({ accessTokenExpiresAtUtc: '2026-01-01T00:01:00.000Z' }))
    setSessionClock({ now: () => Date.parse('2026-01-01T00:00:15.000Z') })

    const pending = apiFetch('/api/notebooks/old-user')
    setSession(
      authSession({
        user: { id: 'u2', email: 'grace@example.com', displayName: 'Grace' },
        accessToken: 'access-b',
        refreshToken: 'refresh-b',
      }),
    )
    release()

    await expect(pending).rejects.toMatchObject({ name: 'AbortError' })
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(getAccessToken()).toBe('access-b')
    expect(getRefreshToken()).toBe('refresh-b')
  })

  it('skips the refresh while the token still has time', async () => {
    const fetchMock = stubFetch(async () => success({ id: 'u1' }))
    setSession(authSession({ accessTokenExpiresAtUtc: '2026-01-01T00:15:00.000Z' }))
    setSessionClock({ now: () => Date.parse('2026-01-01T00:00:15.000Z') })

    await apiFetch('/api/auth/me')

    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})
