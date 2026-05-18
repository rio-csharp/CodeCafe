import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useMe, useLogin, useLogout, AUTH_ME_KEY } from './useAuth'
import { clearCsrfToken } from '../../../lib/apiClient'

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  })
}

describe('useMe', () => {
  beforeEach(() => {
    clearCsrfToken()
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns user data when authenticated', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      text: vi
        .fn()
        .mockResolvedValue(
          JSON.stringify({
            user: { id: '1', email: 'a@b.com', displayName: 'Alice' },
          }),
        ),
    })

    const queryClient = createTestQueryClient()
    const { result } = renderHook(() => useMe(), {
      wrapper: ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      ),
    })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data?.user.displayName).toBe('Alice')
  })

  it('returns null on 401 without retrying', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      status: 401,
      ok: false,
      json: vi.fn().mockResolvedValue({}),
    })

    const queryClient = createTestQueryClient()
    const { result } = renderHook(() => useMe(), {
      wrapper: ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      ),
    })

    await waitFor(() => expect(result.current.isPending).toBe(false))
    expect(result.current.data).toBeNull()
    expect(result.current.isError).toBe(false)
    expect(globalThis.fetch).toHaveBeenCalledTimes(1)
  })
})

describe('useLogin', () => {
  beforeEach(() => {
    clearCsrfToken()
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('caches user data in auth/me on success', async () => {
    globalThis.fetch = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        json: vi.fn().mockResolvedValue({ token: 'csrf-123' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        text: vi
          .fn()
          .mockResolvedValue(
            JSON.stringify({
              user: { id: '1', email: 't@t.com', displayName: 'T' },
            }),
          ),
      })

    const queryClient = createTestQueryClient()
    const { result } = renderHook(() => useLogin(), {
      wrapper: ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      ),
    })

    result.current.mutate({ email: 't@t.com', password: 'pass1234' })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(queryClient.getQueryData(AUTH_ME_KEY)).toEqual({
      user: { id: '1', email: 't@t.com', displayName: 'T' },
    })
  })
})

describe('useLogout', () => {
  beforeEach(() => {
    clearCsrfToken()
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('removes auth/me cache on success', async () => {
    globalThis.fetch = vi
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        json: vi.fn().mockResolvedValue({ token: 'csrf-123' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        text: vi.fn().mockResolvedValue(''),
      })

    const queryClient = createTestQueryClient()
    queryClient.setQueryData(AUTH_ME_KEY, { user: { id: '1' } })

    const { result } = renderHook(() => useLogout(), {
      wrapper: ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
      ),
    })

    result.current.mutate()

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(queryClient.getQueryData(AUTH_ME_KEY)).toBeNull()
  })
})
