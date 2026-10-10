import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { importPage } from './importPage'

const PAGE = {
  id: 'page-9',
  notebookId: 'notebook-1',
  title: 'Imported',
  path: '/imported',
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

describe('importPage', () => {
  it('posts the markdown payload to the notebook import endpoint', async () => {
    await importPage('espresso-notes', {
      fileName: 'v60.md',
      markdown: '# V60\n',
      parentPath: '/brewing',
    })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/notebooks/espresso-notes/pages/import',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ fileName: 'v60.md', markdown: '# V60\n', parentPath: '/brewing' }),
      }),
    )
  })

  it('sends a null parentPath for a root-level import', async () => {
    await importPage('espresso-notes', { fileName: 'v60.md', markdown: '# V60\n', parentPath: null })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      expect.anything(),
      expect.objectContaining({
        body: JSON.stringify({ fileName: 'v60.md', markdown: '# V60\n', parentPath: null }),
      }),
    )
  })

  it('returns the imported page with its new path', async () => {
    const imported = await importPage('espresso-notes', {
      fileName: 'v60.md',
      markdown: '# V60\n',
      parentPath: null,
    })

    expect(imported.path).toBe('/imported')
  })
})
