import type { PagedResult } from '@/shared/api'
import { apiFetch } from '@/shared/api'
import type { NotebookSort, NotebookSummary } from '../model/types'

export const NOTEBOOK_PAGE_SIZE = 12

export interface ListNotebooksParams {
  search: string
  sort: NotebookSort
  page: number
  pageSize?: number
  signal?: AbortSignal
}

/** The anonymous catalog: every public notebook. */
export function listNotebooks({
  search,
  sort,
  page,
  pageSize = NOTEBOOK_PAGE_SIZE,
  signal,
}: ListNotebooksParams): Promise<PagedResult<NotebookSummary>> {
  const trimmed = search.trim()
  const query = new URLSearchParams({
    sort,
    page: String(page),
    pageSize: String(pageSize),
  })
  // An empty search means the whole catalog — sending `search=` would filter on "".
  if (trimmed.length > 0) {
    query.set('search', trimmed)
  }

  return apiFetch<PagedResult<NotebookSummary>>(`/api/notebooks/public?${query.toString()}`, { signal })
}
