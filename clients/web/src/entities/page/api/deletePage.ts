import { apiFetch } from '@/shared/api'

/** Soft delete: the page (and its subpages) wait in the trash for a restore. */
export function deletePage(pageId: string): Promise<null> {
  return apiFetch<null>(`/api/pages/${encodeURIComponent(pageId)}`, { method: 'DELETE' })
}
