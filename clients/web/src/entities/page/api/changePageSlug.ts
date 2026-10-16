import { apiFetch } from '@/shared/api'
import type { PageDetails } from '../model/types'

/**
 * Replaces a page's URL slug; the answer carries its new path.
 *
 * Omit `slug` to have the server derive one from the page title — the notebook is
 * the only place that owns the slug rule (Unicode letters kept, 80-char cap,
 * random suffix on a clash), so callers never hand-roll a slug for this.
 */
export function changePageSlug(pageId: string, slug?: string): Promise<PageDetails> {
  return apiFetch<PageDetails>(`/api/pages/${encodeURIComponent(pageId)}/slug`, {
    method: 'POST',
    body: JSON.stringify({ slug: slug ?? null }),
  })
}
