import { accessCodeHeaderFor, apiFetch } from '@/shared/api'
import type { PageRevisionSnapshot } from '../model/types'

/** The page's reconstructed block state at `atUtc`; read access suffices. */
export function getPageAtRevision(
  pageId: string,
  atUtc: string,
  signal?: AbortSignal,
  slug?: string,
): Promise<PageRevisionSnapshot> {
  return apiFetch<PageRevisionSnapshot>(
    `/api/pages/${encodeURIComponent(pageId)}/revisions/at?atUtc=${encodeURIComponent(atUtc)}`,
    { signal, headers: slug === undefined ? undefined : accessCodeHeaderFor(slug) },
  )
}
