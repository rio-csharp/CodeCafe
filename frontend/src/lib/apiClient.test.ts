import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiGet, checkBackendHealth } from './apiClient'

describe('apiGet', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('returns parsed JSON when the request succeeds', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ name: 'CodeCafe' }), {
        headers: { 'Content-Type': 'application/json' },
        status: 200,
      }),
    )

    await expect(apiGet<{ name: string }>('/api/system/info')).resolves.toEqual({
      name: 'CodeCafe',
    })
  })

  it('throws when the request fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 500 }))

    await expect(apiGet('/api/system/info')).rejects.toThrow(
      'API request failed with status 500',
    )
  })
})

describe('checkBackendHealth', () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('returns true when the health endpoint succeeds', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('Healthy', { status: 200 }))

    await expect(checkBackendHealth()).resolves.toBe(true)
  })

  it('returns false when the health endpoint fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 503 }))

    await expect(checkBackendHealth()).resolves.toBe(false)
  })
})
