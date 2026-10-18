import { accessCodeHeaderFor, apiFetch } from '@/shared/api'
import type { BlockRevision, CursorPage } from '../model/types'

export interface ListBlockRevisionsParams {
  pageId: string
  blockId: string
  /** Notebook slug, so page-id requests carry the access code those reads need. */
  slug?: string
  cursor?: string | null
  signal?: AbortSignal
}

/** One block's revision log, newest-first; cursor-paginated. */
export function listBlockRevisions({
  pageId,
  blockId,
  slug,
  cursor = null,
  signal,
}: ListBlockRevisionsParams): Promise<CursorPage<BlockRevision>> {
  const query = cursor !== null ? `?cursor=${encodeURIComponent(cursor)}` : ''
  return apiFetch<CursorPage<BlockRevision>>(
    `/api/pages/${encodeURIComponent(pageId)}/blocks/${encodeURIComponent(blockId)}/revisions${query}`,
    { signal, headers: slug === undefined ? undefined : accessCodeHeaderFor(slug) },
  )
}
