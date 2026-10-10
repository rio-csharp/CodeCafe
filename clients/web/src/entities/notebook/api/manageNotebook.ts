import { apiFetch } from '@/shared/api'
import type { NotebookDetails, NotebookVisibility } from '../model/types'

export type CollaboratorRole = 'Viewer' | 'Editor'

export interface NotebookPatch {
  title?: string
  description?: string | null
  visibility?: NotebookVisibility
}

/** Partial update; untouched fields stay as they are. Returns the fresh details. */
export function updateNotebook(idOrSlug: string, patch: NotebookPatch): Promise<NotebookDetails> {
  return apiFetch<NotebookDetails>(`/api/notebooks/${encodeURIComponent(idOrSlug)}`, {
    method: 'PATCH',
    body: JSON.stringify(patch),
  })
}

/** Shares the notebook with a registered user by email. */
export function shareNotebook(
  idOrSlug: string,
  email: string,
  role: CollaboratorRole,
): Promise<null> {
  return apiFetch<null>(`/api/notebooks/${encodeURIComponent(idOrSlug)}/shares`, {
    method: 'POST',
    body: JSON.stringify({ email, role }),
  })
}

export function revokeNotebookShare(idOrSlug: string, userId: string): Promise<null> {
  return apiFetch<null>(
    `/api/notebooks/${encodeURIComponent(idOrSlug)}/shares/${encodeURIComponent(userId)}`,
    { method: 'DELETE' },
  )
}

/** A null code removes the lock; anything else (re)sets it. */
export function setNotebookAccessCode(idOrSlug: string, accessCode: string | null): Promise<null> {
  return apiFetch<null>(`/api/notebooks/${encodeURIComponent(idOrSlug)}/access-code`, {
    method: 'POST',
    body: JSON.stringify({ accessCode }),
  })
}

/** Replaces the whole tag set — the backend stores exactly what it receives. */
export function setNotebookTags(idOrSlug: string, tags: string[]): Promise<null> {
  return apiFetch<null>(`/api/notebooks/${encodeURIComponent(idOrSlug)}/tags`, {
    method: 'PUT',
    body: JSON.stringify({ tags }),
  })
}

/** Moves the notebook to a new URL slug; owners only. Returns the fresh details. */
export function changeNotebookSlug(idOrSlug: string, slug: string): Promise<NotebookDetails> {
  return apiFetch<NotebookDetails>(`/api/notebooks/${encodeURIComponent(idOrSlug)}/slug`, {
    method: 'POST',
    body: JSON.stringify({ slug }),
  })
}
