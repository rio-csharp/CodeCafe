import { apiFetch } from '@/shared/api'

/**
 * Rolls the page back to its state at `atUtc`. The restore itself is recorded
 * as a new batch, so it can be undone by restoring again.
 */
export function restorePageRevision(pageId: string, atUtc: string): Promise<void> {
  return apiFetch<void>(`/api/pages/${encodeURIComponent(pageId)}/revisions/restore`, {
    method: 'POST',
    body: JSON.stringify({ atUtc }),
  })
}
