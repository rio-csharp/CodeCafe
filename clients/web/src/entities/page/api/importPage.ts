import { apiFetch } from '@/shared/api'
import type { PageDetails } from '../model/types'

export interface ImportPageData {
  fileName: string
  markdown: string
  /** Parent page's path; null imports at the notebook root. */
  parentPath: string | null
}

/** Turns a markdown file into a page; the answer carries its new path. */
export function importPage(idOrSlug: string, data: ImportPageData): Promise<PageDetails> {
  return apiFetch<PageDetails>(`/api/notebooks/${encodeURIComponent(idOrSlug)}/pages/import`, {
    method: 'POST',
    body: JSON.stringify(data),
  })
}
