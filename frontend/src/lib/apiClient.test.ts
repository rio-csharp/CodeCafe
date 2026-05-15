import { afterEach, describe, expect, it, vi } from 'vitest'
import { apiDelete, apiJson, apiSend, checkBackendHealth } from './apiClient'

describe('apiClient', () => {
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

  it('returns parsed JSON for successful requests', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(Response.json({ value: 42 }))

    await expect(apiJson<{ value: number }>('/api/example')).resolves.toEqual({ value: 42 })
  })

  it('throws when a JSON request fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 500 }))

    await expect(apiJson('/api/example')).rejects.toThrow('API request failed with status 500')
  })

  it('sends JSON payloads with apiSend', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(Response.json({ ok: true }))

    await expect(apiSend('/api/example', 'PUT', { value: 42 })).resolves.toEqual({ ok: true })
    expect(fetchSpy).toHaveBeenCalledWith(
      'http://localhost:5000/api/example',
      expect.objectContaining({
        body: JSON.stringify({ value: 42 }),
        credentials: 'include',
        method: 'PUT',
      }),
    )
  })

  it('deletes successfully with apiDelete', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 204 }))

    await expect(apiDelete('/api/example')).resolves.toBeUndefined()
  })

  it('throws when apiDelete fails', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 403 }))

    await expect(apiDelete('/api/example')).rejects.toThrow('API request failed with status 403')
  })
})
