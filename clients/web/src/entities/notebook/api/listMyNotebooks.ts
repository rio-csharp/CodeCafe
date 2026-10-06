import type { PagedResult } from '@/shared/api'
import { apiFetch } from '@/shared/api'
import { NOTEBOOK_PAGE_SIZE } from './listNotebooks'
import type { NotebookSort, NotebookSummary } from '../model/types'

export interface ListMyNotebooksParams {
  page: number
  pageSize?: number
  sort?: NotebookSort
  signal?: AbortSignal
}

/** Own + shared notebooks of the signed-in user; the public catalog is elsewhere. */
export function listMyNotebooks({
  page,
  pageSize = NOTEBOOK_PAGE_SIZE,
  sort = 'UpdatedDesc',
  signal,
}: ListMyNotebooksParams): Promise<PagedResult<NotebookSummary>> {
  const query = new URLSearchParams({
    sort,
    page: String(page),
    pageSize: String(pageSize),
  })

  return apiFetch<PagedResult<NotebookSummary>>(`/api/notebooks?${query.toString()}`, { signal })
}
