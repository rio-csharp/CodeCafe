import { apiFetch } from '@/shared/api'

/** One favorited page, with everything a link to it needs. */
export interface FavoritePageEntry {
  pageId: string
  title: string
  /** Slug chain built by the server, e.g. `/setup/rust-notes`. */
  path: string
  notebookId: string
  notebookTitle: string
  notebookSlug: string
}

export interface ListFavoritePagesParams {
  /** Narrows the list to one notebook; omit for every notebook. */
  notebookId?: string
  signal?: AbortSignal
}

export function listFavoritePages(
  params: ListFavoritePagesParams = {},
): Promise<FavoritePageEntry[]> {
  const search =
    params.notebookId === undefined
      ? ''
      : `?notebookId=${encodeURIComponent(params.notebookId)}`
  return apiFetch<FavoritePageEntry[]>(`/api/pages/favorites${search}`, {
    signal: params.signal,
  })
}
