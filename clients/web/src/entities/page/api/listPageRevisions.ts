import { apiFetch } from '@/shared/api'
import type { CursorPage, PageRevisionGroup } from '../model/types'

export interface ListPageRevisionsParams {
  pageId: string
  cursor?: string | null
  signal?: AbortSignal
}

/** Newest-first, grouped by save batch; cursor-paginated. */
export function listPageRevisions({
  pageId,
  cursor = null,
  signal,
}: ListPageRevisionsParams): Promise<CursorPage<PageRevisionGroup>> {
  const query = cursor !== null ? `?cursor=${encodeURIComponent(cursor)}` : ''
  return apiFetch<CursorPage<PageRevisionGroup>>(
    `/api/pages/${encodeURIComponent(pageId)}/revisions${query}`,
    { signal },
  )
}
