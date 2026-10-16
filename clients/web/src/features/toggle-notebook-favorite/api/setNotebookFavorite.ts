import { apiFetch } from '@/shared/api'

/** Explicit set, not a toggle: the caller passes the state it wants. */
export function setNotebookFavorite(notebookId: string, isFavorite: boolean): Promise<null> {
  return apiFetch<null>(`/api/notebooks/${encodeURIComponent(notebookId)}/favorite`, {
    method: 'POST',
    body: JSON.stringify({ isFavorite }),
  })
}
