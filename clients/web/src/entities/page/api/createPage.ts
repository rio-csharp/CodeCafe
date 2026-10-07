import { apiFetch } from '@/shared/api'
import type { PageDetails } from '../model/types'

export interface CreatePageParams {
  slug: string
  title: string
  /** Parent page path for nesting; null creates a root page. */
  parentPath?: string | null
}

/** Requires write access; returns the created page with its (empty) blocks. */
export function createPage({ slug, title, parentPath = null }: CreatePageParams): Promise<PageDetails> {
  return apiFetch<PageDetails>(`/api/notebooks/${encodeURIComponent(slug)}/pages`, {
    method: 'POST',
    body: JSON.stringify({ title, parentPath }),
  })
}
