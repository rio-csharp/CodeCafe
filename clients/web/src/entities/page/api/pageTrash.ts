import type { PagedResult } from '@/shared/api'
import { apiFetch } from '@/shared/api'

export interface TrashedPageEntry {
  pageId: string
  title: string
  slug: string
  /** How many subpages went to the trash along with it. */
  descendantCount: number
  deletedAtUtc: string
}

export interface ListTrashedPagesParams {
  slug: string
  page?: number
  pageSize?: number
  signal?: AbortSignal
}

/** Pages soft-deleted from one notebook. */
export function listTrashedPages({
  slug,
  page = 1,
  pageSize = 50,
  signal,
}: ListTrashedPagesParams): Promise<PagedResult<TrashedPageEntry>> {
  return apiFetch<PagedResult<TrashedPageEntry>>(
    `/api/notebooks/${encodeURIComponent(slug)}/trash?page=${page}&pageSize=${pageSize}`,
    { signal },
  )
}

/** Soft delete is reversible: this puts the page back on the menu. */
export function restoreTrashedPage(pageId: string): Promise<null> {
  return apiFetch<null>(`/api/trash/pages/${encodeURIComponent(pageId)}/restore`, {
    method: 'POST',
  })
}

/** Permanent. There is no undo past this one. */
export function purgeTrashedPage(pageId: string): Promise<null> {
  return apiFetch<null>(`/api/trash/pages/${encodeURIComponent(pageId)}`, { method: 'DELETE' })
}
