import { apiFetch } from '@/shared/api'

/** Explicit set, not a toggle: the caller passes the state it wants. */
export function setPageFavorite(pageId: string, isFavorite: boolean): Promise<null> {
  return apiFetch<null>(`/api/pages/${encodeURIComponent(pageId)}/favorite`, {
    method: 'POST',
    body: JSON.stringify({ isFavorite }),
  })
}
