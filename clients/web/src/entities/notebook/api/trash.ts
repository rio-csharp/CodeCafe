import type { PagedResult } from '@/shared/api'
import { apiFetch } from '@/shared/api'

export interface TrashEntry {
  notebookId: string
  title: string
  pageCount: number
  deletedAtUtc: string
}

/** The signed-in user's trashed notebooks. */
export function listTrash(signal?: AbortSignal): Promise<PagedResult<TrashEntry>> {
  return apiFetch<PagedResult<TrashEntry>>('/api/trash?page=1&pageSize=50', { signal })
}

/** Soft-delete is reversible: this brings the notebook back to its shelf. */
export function restoreTrashedNotebook(notebookId: string): Promise<null> {
  return apiFetch<null>(`/api/trash/${encodeURIComponent(notebookId)}/restore`, {
    method: 'POST',
  })
}

/** Permanent. There is no undo past this one. */
export function purgeTrashedNotebook(notebookId: string): Promise<null> {
  return apiFetch<null>(`/api/trash/${encodeURIComponent(notebookId)}`, { method: 'DELETE' })
}

/** Purges every trashed notebook at once. */
export function emptyTrash(): Promise<null> {
  return apiFetch<null>('/api/trash', { method: 'DELETE' })
}

/** Deleting a notebook is a soft delete: it lands in the trash. */
export function deleteNotebook(notebookId: string): Promise<null> {
  return apiFetch<null>(`/api/notebooks/${encodeURIComponent(notebookId)}`, { method: 'DELETE' })
}
