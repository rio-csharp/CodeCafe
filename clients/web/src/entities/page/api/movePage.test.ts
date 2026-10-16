import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { movePage } from './movePage'

const PAGE = {
  id: 'page-1',
  notebookId: 'notebook-1',
  title: 'Setup',
  path: '/guide/setup',
  isArchived: false,
  isFavorite: false,
  blocks: [],
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  updatedAtUtc: '2026-01-01T00:00:00.000Z',
}

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn(
      async () =>
        new Response(JSON.stringify({ value: PAGE, error: null, isSuccess: true }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
    ),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('movePage', () => {
  it('posts the move payload to the page move endpoint', async () => {
    await movePage('page-1', { parentPath: '/guide', afterPageId: 'page-9' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/page-1/move',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ parentPath: '/guide', afterPageId: 'page-9' }),
      }),
    )
  })

  it('sends nulls verbatim: root parent, first position', async () => {
    await movePage('page-1', { parentPath: null, afterPageId: null })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/page-1/move',
      expect.objectContaining({
        body: JSON.stringify({ parentPath: null, afterPageId: null }),
      }),
    )
  })

  it('returns the moved page with its new path', async () => {
    const moved = await movePage('page-1', { parentPath: null, afterPageId: null })

    expect(moved.path).toBe('/guide/setup')
  })
})
