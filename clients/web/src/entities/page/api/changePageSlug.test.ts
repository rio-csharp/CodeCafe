import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { changePageSlug } from './changePageSlug'

const PAGE = {
  id: 'page-1',
  notebookId: 'notebook-1',
  title: 'Café Notes',
  path: '/café-notes',
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

describe('changePageSlug', () => {
  it('posts an explicit slug to the page slug endpoint', async () => {
    await changePageSlug('page-1', 'café-notes')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/page-1/slug',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ slug: 'café-notes' }),
      }),
    )
  })

  it('sends null when the server should derive the slug from the title', async () => {
    await changePageSlug('page-1')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/pages/page-1/slug',
      expect.objectContaining({
        body: JSON.stringify({ slug: null }),
      }),
    )
  })

  it('returns the page with its new path', async () => {
    const renamed = await changePageSlug('page-1')

    expect(renamed.path).toBe('/café-notes')
  })
})
