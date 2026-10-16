import { apiFetch } from '@/shared/api'
import type { PageDetails } from '../model/types'

export interface UpdatePageData {
  title?: string
  isArchived?: boolean
}

/** Title/archive edits only — content moves through the block-ops endpoints. */
export function updatePage(pageId: string, data: UpdatePageData): Promise<PageDetails> {
  return apiFetch<PageDetails>(`/api/pages/${encodeURIComponent(pageId)}`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  })
}
