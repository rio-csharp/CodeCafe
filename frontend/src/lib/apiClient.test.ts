import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { apiFetch, ApiError, clearCsrfToken, fetchCsrfToken } from './apiClient'

describe('ApiError', () => {
  it('stores status and message', () => {
    const err = new ApiError(403, 'Forbidden')
    expect(err.status).toBe(403)
    expect(err.message).toBe('Forbidden')
    expect(err.name).toBe('ApiError')
  })
})

describe('apiFetch', () => {
  beforeEach(() => {
    clearCsrfToken()
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('does not attach CSRF token for GET requests', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      text: vi.fn().mockResolvedValue(JSON.stringify({ data: 'ok' })),
    })

    await apiFetch('/test', { method: 'GET' })

    expect(globalThis.fetch).toHaveBeenCalledOnce()
    const [, options] = (globalThis.fetch as ReturnType<typeof vi.fn>).mock.calls[0]
    expect(options.headers.get('X-CSRF-TOKEN')).toBeNull()
  })

  it('fetches CSRF token and attaches it for POST requests', async () => {
    const mockFetch = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: vi.fn().mockResolvedValue({ token: 'csrf-123' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        text: vi.fn().mockResolvedValue(JSON.stringify({ success: true })),
      })

    globalThis.fetch = mockFetch

    await apiFetch('/test', { method: 'POST', body: JSON.stringify({}) })

    expect(mockFetch).toHaveBeenCalledTimes(2)

    const [csrfUrl] = mockFetch.mock.calls[0]
    expect(csrfUrl).toContain('/api/auth/csrf')

    const [, options] = mockFetch.mock.calls[1]
    expect(options.headers.get('X-CSRF-TOKEN')).toBe('csrf-123')
  })

  it('reuses cached CSRF token for subsequent mutating requests', async () => {
    const mockFetch = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: vi.fn().mockResolvedValue({ token: 'cached-token' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        text: vi.fn().mockResolvedValue(''),
      })
      .mockResolvedValueOnce({
        ok: true,
        text: vi.fn().mockResolvedValue(''),
      })

    globalThis.fetch = mockFetch

    await apiFetch('/first', { method: 'POST', body: '{}' })
    await apiFetch('/second', { method: 'POST', body: '{}' })

    expect(mockFetch).toHaveBeenCalledTimes(3)
    const [, secondOptions] = mockFetch.mock.calls[2]
    expect(secondOptions.headers.get('X-CSRF-TOKEN')).toBe('cached-token')
  })

  it('returns undefined for empty body responses', async () => {
    globalThis.fetch = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: vi.fn().mockResolvedValue({ token: 'csrf-empty' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        text: vi.fn().mockResolvedValue(''),
      })

    const result = await apiFetch<void>('/test', { method: 'POST' })
    expect(result).toBeUndefined()
  })

  it('throws ApiError on non-ok response', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 422,
      json: vi.fn().mockResolvedValue({ message: 'Invalid input' }),
    })

    await expect(apiFetch('/test', { method: 'GET' })).rejects.toThrow(ApiError)
    await expect(apiFetch('/test', { method: 'GET' })).rejects.toThrow(
      'Invalid input',
    )
  })
})

describe('fetchCsrfToken', () => {
  beforeEach(() => {
    clearCsrfToken()
    vi.stubGlobal('fetch', vi.fn())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('fetches and caches the token', async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: vi.fn().mockResolvedValue({ token: 'abc-xyz' }),
    })

    const token = await fetchCsrfToken()
    expect(token).toBe('abc-xyz')

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: vi.fn().mockResolvedValue({ token: 'should-not-use' }),
    })
    const cached = await fetchCsrfToken()
    expect(cached).toBe('abc-xyz')
  })
})
