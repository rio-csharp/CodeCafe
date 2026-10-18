import { accessCodeHeaderFor, apiFetch } from '@/shared/api'

/** Explicit set, not a toggle: the caller passes the state it wants. */
export function setPageFavorite(pageId: string, isFavorite: boolean, slug?: string): Promise<null> {
  return apiFetch<null>(`/api/pages/${encodeURIComponent(pageId)}/favorite`, {
    method: 'POST',
    headers: slug === undefined ? undefined : accessCodeHeaderFor(slug),
    body: JSON.stringify({ isFavorite }),
  })
}
