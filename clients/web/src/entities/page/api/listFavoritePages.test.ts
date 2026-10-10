import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { listFavoritePages } from './listFavoritePages'

const ENTRIES = [
  {
    pageId: 'page-1',
    title: 'V60',
    path: '/brewing/v60',
    notebookId: 'nb-1',
    notebookTitle: 'Guides',
    notebookSlug: 'guides',
  },
]

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn(
      async () =>
        new Response(JSON.stringify({ value: ENTRIES, error: null, isSuccess: true }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
    ),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('listFavoritePages', () => {
  it('lists every favorite when no notebook is given', async () => {
    const entries = await listFavoritePages()

    expect(vi.mocked(fetch)).toHaveBeenCalledWith('/api/pages/favorites', expect.anything())
    expect(entries).toEqual(ENTRIES)
  })

  it('narrows the list with the notebookId query parameter', async () => {
    await listFavoritePages({ notebookId: 'nb-1' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/favorites?notebookId=nb-1',
      expect.anything(),
    )
  })

  it('forwards the abort signal', async () => {
    const controller = new AbortController()

    await listFavoritePages({ signal: controller.signal })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({ signal: expect.any(AbortSignal) }),
    )
  })
})
