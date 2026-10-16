import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getPageByPath } from './getPageByPath'

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

describe('getPageByPath', () => {
  it('asks for the page by the path the tree reports', async () => {
    await getPageByPath({ slug: 'role-smoke', path: '/guide/setup' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/notebooks/role-smoke/pages/by-path?path=/guide/setup',
      expect.anything(),
    )
  })

  it('keeps path separators literal so the server can split them', async () => {
    // `URLSearchParams` would encode these slashes as %2F, which the API 404s.
    await getPageByPath({ slug: 'role-smoke', path: 'guide/setup' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/notebooks/role-smoke/pages/by-path?path=/guide/setup',
      expect.anything(),
    )
  })

  it('encodes each segment, so CJK slugs survive the round trip', async () => {
    await getPageByPath({ slug: '我的笔记本', path: '/指南/深烘' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      `/api/notebooks/${encodeURIComponent('我的笔记本')}/pages/by-path?path=/${encodeURIComponent('指南')}/${encodeURIComponent('深烘')}`,
      expect.anything(),
    )
  })
})
