import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/shared/api'
import { changeNotebookSlug } from './manageNotebook'

const NOTEBOOK = {
  id: 'nb-1',
  title: 'Espresso Notes',
  description: null,
  slug: 'filter-notes',
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

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('changeNotebookSlug', () => {
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

  it('posts the new slug to the slug endpoint', async () => {
    await changeNotebookSlug('espresso-notes', 'filter-notes')

    expect(vi.mocked(fetch)).toHaveBeenCalledWith(
      '/api/notebooks/espresso-notes/slug',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ slug: 'filter-notes' }),
      }),
    )
  })

  it('returns the fresh details carrying the new slug', async () => {
    const notebook = await changeNotebookSlug('espresso-notes', 'filter-notes')

    expect(notebook.slug).toBe('filter-notes')
  })

  it('surfaces slug_already_taken as an ApiError', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(
        async () =>
          new Response(
            JSON.stringify({
              value: null,
              error: {
                code: 'slug_already_taken',
                message: 'A notebook with this slug already exists.',
                kind: 'Conflict',
              },
              isSuccess: false,
            }),
            { status: 409, headers: { 'content-type': 'application/json' } },
          ),
      ),
    )

    const failure = await changeNotebookSlug('espresso-notes', 'taken').catch(
      (error: unknown) => error,
    )

    expect(failure).toBeInstanceOf(ApiError)
    expect((failure as ApiError).status).toBe(409)
    expect((failure as ApiError).code).toBe('slug_already_taken')
  })
})
