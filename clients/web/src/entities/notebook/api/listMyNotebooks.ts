import type { PagedResult } from '@/shared/api'
import { apiFetch } from '@/shared/api'
import { NOTEBOOK_PAGE_SIZE } from './listNotebooks'
import type { NotebookSort, NotebookSummary, NotebookVisibility } from '../model/types'

export interface ListMyNotebooksParams {
  page: number
  pageSize?: number
  sort?: NotebookSort
  search?: string
  favoritesOnly?: boolean
  visibility?: NotebookVisibility | null
  signal?: AbortSignal
}

/**
 * Own + shared notebooks of the signed-in user; the public catalog is
 * elsewhere. Supports the ownership-side filters the public list lacks.
 */
export function listMyNotebooks({
  page,
  pageSize = NOTEBOOK_PAGE_SIZE,
  sort = 'UpdatedDesc',
  search = '',
  favoritesOnly = false,
  visibility = null,
  signal,
}: ListMyNotebooksParams): Promise<PagedResult<NotebookSummary>> {
  const query = new URLSearchParams({
    sort,
    page: String(page),
    pageSize: String(pageSize),
  })
  const trimmed = search.trim()
  if (trimmed.length > 0) {
    query.set('search', trimmed)
  }
  if (favoritesOnly) {
    query.set('isFavorite', 'true')
  }
  if (visibility !== null) {
    query.set('visibility', visibility)
  }

  return apiFetch<PagedResult<NotebookSummary>>(`/api/notebooks?${query.toString()}`, { signal })
}
