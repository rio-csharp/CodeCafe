import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { listTrashedPages, purgeTrashedPage, restoreTrashedPage } from './pageTrash'

const PAGED = {
  items: [
    {
      pageId: 'page-1',
      title: 'Old brew log',
      slug: 'old-brew-log',
      descendantCount: 2,
      deletedAtUtc: '2026-01-02T00:00:00.000Z',
    },
  ],
  page: 1,
  pageSize: 50,
  totalCount: 1,
  hasNextPage: false,
}

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn(
      async () =>
        new Response(JSON.stringify({ value: PAGED, error: null, isSuccess: true }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
    ),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('listTrashedPages', () => {
  it('asks the notebook trash endpoint with paging', async () => {
    const result = await listTrashedPages({ slug: 'espresso-notes' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/notebooks/espresso-notes/trash?page=1&pageSize=50',
      expect.anything(),
    )
    expect(result.items[0]?.title).toBe('Old brew log')
  })
})

describe('restoreTrashedPage', () => {
  it('posts to the page trash restore endpoint', async () => {
    await restoreTrashedPage('page-1')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/trash/pages/page-1/restore',
      expect.objectContaining({ method: 'POST' }),
    )
  })
})

describe('purgeTrashedPage', () => {
  it('deletes the page trash entry forever', async () => {
    await purgeTrashedPage('page-1')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/trash/pages/page-1',
      expect.objectContaining({ method: 'DELETE' }),
    )
  })
})
