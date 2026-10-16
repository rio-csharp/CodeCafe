import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { searchPages } from './searchPages'

const HITS = {
  items: [
    {
      pageId: 'page-1',
      notebookId: 'nb-1',
      notebookSlug: 'guides',
      notebookTitle: 'Guides',
      title: 'V60',
      path: '/brewing/v60',
      snippet: '…pour in circles…',
    },
  ],
  nextCursor: 'cursor-2',
}

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn(
      async () =>
        new Response(JSON.stringify({ value: HITS, error: null, isSuccess: true }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
    ),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('searchPages', () => {
  it('sends the query without a cursor on the first page', async () => {
    const result = await searchPages({ query: 'v60' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith('/api/search?q=v60', expect.anything())
    expect(result).toEqual(HITS)
  })

  it('passes the cursor through for the next page', async () => {
    await searchPages({ query: 'v60', cursor: 'cursor-2' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/search?q=v60&cursor=cursor-2',
      expect.anything(),
    )
  })

  it('percent-encodes a CJK query', async () => {
    await searchPages({ query: '深烘' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      `/api/search?q=${encodeURIComponent('深烘')}`,
      expect.anything(),
    )
  })
})
