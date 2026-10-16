import { apiFetch } from '@/shared/api'
import type { PageDetails } from '../model/types'

export interface MovePageData {
  /** New parent's path; null re-roots the page. */
  parentPath: string | null
  /** Sibling to slot in after; null takes the first position. */
  afterPageId: string | null
}

/** Reparent and/or reorder a page; the answer carries its new path. */
export function movePage(pageId: string, data: MovePageData): Promise<PageDetails> {
  return apiFetch<PageDetails>(`/api/pages/${encodeURIComponent(pageId)}/move`, {
    method: 'POST',
    body: JSON.stringify(data),
  })
}
