import { apiFetch } from '@/shared/api'
import type { CursorPage } from '../model/types'

export interface PageSearchHit {
  pageId: string
  notebookId: string
  /** The reader link needs the slug; the title is just display text. */
  notebookSlug: string
  notebookTitle: string
  title: string
  path: string
  /** Empty for title-only matches — the caller hides it then. */
  snippet: string
}

export interface SearchPagesParams {
  query: string
  cursor?: string | null
  signal?: AbortSignal
}

/**
 * Full-text search across every page the caller can read (own + shared
 * notebooks). Auth-only on the server. Keyset-paginated: pass the previous
 * page's `nextCursor` to continue.
 */
export function searchPages({
  query,
  cursor = null,
  signal,
}: SearchPagesParams): Promise<CursorPage<PageSearchHit>> {
  const params = new URLSearchParams({ q: query })
  if (cursor !== null && cursor !== undefined) {
    params.set('cursor', cursor)
  }
  return apiFetch<CursorPage<PageSearchHit>>(`/api/search?${params.toString()}`, { signal })
}
