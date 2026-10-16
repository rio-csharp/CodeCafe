import { apiFetch } from '@/shared/api'

export type PageShareRole = 'Viewer' | 'Editor'

/** Shares one page (and implicitly its subtree) with a registered user by email. */
export function sharePage(pageId: string, email: string, role: PageShareRole): Promise<null> {
  return apiFetch<null>(`/api/pages/${encodeURIComponent(pageId)}/shares`, {
    method: 'POST',
    body: JSON.stringify({ email, role }),
  })
}

export function revokePageShare(pageId: string, userId: string): Promise<null> {
  return apiFetch<null>(
    `/api/pages/${encodeURIComponent(pageId)}/shares/${encodeURIComponent(userId)}`,
    { method: 'DELETE' },
  )
}
