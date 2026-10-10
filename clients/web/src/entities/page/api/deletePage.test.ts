import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { deletePage } from './deletePage'

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn(
      async () =>
        new Response(JSON.stringify({ value: null, error: null, isSuccess: true }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
    ),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('deletePage', () => {
  it('issues a DELETE against the page endpoint', async () => {
    await deletePage('page-1')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/page-1',
      expect.objectContaining({ method: 'DELETE' }),
    )
  })
})
