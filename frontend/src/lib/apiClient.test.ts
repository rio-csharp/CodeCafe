import { afterEach, describe, expect, it, vi } from 'vitest'
import { checkBackendHealth } from './apiClient'

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
