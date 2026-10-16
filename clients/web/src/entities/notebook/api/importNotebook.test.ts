import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { importNotebook } from './importNotebook'

const NOTEBOOK = {
  id: 'nb-9',
  title: 'Imported',
  description: null,
  slug: 'imported',
  visibility: 'Private',
  hasAccessCode: false,
  tags: [],
  shares: [],
  pageCount: 1,
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  updatedAtUtc: '2026-01-01T00:00:00.000Z',
  isOwner: true,
  canWrite: true,
}

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn(
      async () =>
        new Response(JSON.stringify({ value: NOTEBOOK, error: null, isSuccess: true }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
    ),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('importNotebook', () => {
  it('posts the markdown payload to the import endpoint', async () => {
    await importNotebook({ fileName: 'recipes.md', markdown: '# Recipes\n' })

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/notebooks/import',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ fileName: 'recipes.md', markdown: '# Recipes\n' }),
      }),
    )
  })

  it('returns the created notebook with its slug', async () => {
    const notebook = await importNotebook({ fileName: 'recipes.md', markdown: '# Recipes\n' })

    expect(notebook.slug).toBe('imported')
  })
})
