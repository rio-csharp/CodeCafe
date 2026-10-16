import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { setPageFavorite } from './setPageFavorite'

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

describe('setPageFavorite', () => {
  it('posts the explicit state to the page favorite endpoint', async () => {
    await setPageFavorite('page-1', true)

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/page-1/favorite',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ isFavorite: true }),
      }),
    )
  })

  it('posts false verbatim when unfavoriting', async () => {
    await setPageFavorite('page-1', false)

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/page-1/favorite',
      expect.objectContaining({
        body: JSON.stringify({ isFavorite: false }),
      }),
    )
  })

  it('encodes the page id', async () => {
    await setPageFavorite('page/with slash', true)

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/page%2Fwith%20slash/favorite',
      expect.anything(),
    )
  })
})
