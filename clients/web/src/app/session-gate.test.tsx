import { useQuery } from '@tanstack/react-query'
import { act, render, screen } from '@testing-library/react'
import { createMemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { apiFetch, clearSession, REFRESH_STORAGE_KEY, setSession } from '@/shared/api'
import type { AuthSessionDto } from '@/shared/api'
import { Providers } from './providers'

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const SESSION: AuthSessionDto = {
  user: USER,
  accessToken: 'access-1',
  accessTokenExpiresAtUtc: '2099-01-01T00:00:00.000Z',
  refreshToken: 'refresh-2',
}

function envelope(value: unknown): Response {
  return new Response(JSON.stringify({ value, error: null, isSuccess: true }), {
    status: 200,
    headers: { 'content-type': 'application/json' },
  })
}

/** Stands in for the reader: the answer depends on whether the request carried a token. */
function Probe({ name }: { name: string }) {
  const { data } = useQuery({
    queryKey: ['probe', name],
    queryFn: () => apiFetch<string>('/api/probe'),
  })

  return <p>{data ?? 'waiting'}</p>
}

function answerByToken(init: RequestInit | undefined): Response {
  const headers = new Headers(init?.headers)
  return envelope(
    headers.get('Authorization') === `Bearer ${SESSION.accessToken}` ? 'authorized' : 'anonymous',
  )
}

beforeEach(() => {
  window.localStorage.clear()
  clearSession()
  useSessionStore.setState({ status: 'unknown', user: null })
  vi.unstubAllGlobals()
})

describe('session gating', () => {
  it('holds the routes back until a stored session is resumed', async () => {
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')

    let resolveRefresh: (response: Response) => void = () => undefined
    const pendingRefresh = new Promise<Response>((resolve) => {
      resolveRefresh = resolve
    })
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input).endsWith('/api/auth/refresh')) {
        return pendingRefresh
      }
      return answerByToken(init)
    })
    vi.stubGlobal('fetch', fetchMock)

    const router = createMemoryRouter([{ path: '/', element: <Probe name="gate" /> }])
    render(<Providers router={router} />)

    // Only the refresh is out: no route has mounted, so nothing has asked the API anonymously.
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(String(fetchMock.mock.calls[0]?.[0])).toContain('/api/auth/refresh')
    expect(screen.queryByText('waiting')).not.toBeInTheDocument()

    await act(async () => {
      resolveRefresh(envelope(SESSION))
    })

    expect(await screen.findByText('authorized')).toBeInTheDocument()
  })

  it('drops the answers an anonymous visitor cached when someone signs in', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async (_input: RequestInfo | URL, init?: RequestInit) => answerByToken(init)),
    )

    const router = createMemoryRouter([{ path: '/', element: <Probe name="sign-in" /> }])
    render(<Providers router={router} />)

    expect(await screen.findByText('anonymous')).toBeInTheDocument()

    await act(async () => {
      setSession(SESSION)
      useSessionStore.getState().setAuthenticated(USER)
    })

    expect(await screen.findByText('authorized')).toBeInTheDocument()
  })
})
