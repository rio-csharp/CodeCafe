import { apiFetch } from '@/shared/api'

/**
 * Rolls one block back to `blockVersion`. Like a page restore, the restore is
 * itself recorded as a new revision, so it stays undoable.
 */
export function restoreBlockRevision(
  pageId: string,
  blockId: string,
  blockVersion: number,
): Promise<void> {
  return apiFetch<void>(
    `/api/pages/${encodeURIComponent(pageId)}/blocks/${encodeURIComponent(blockId)}/revisions/restore`,
    {
      method: 'POST',
      body: JSON.stringify({ blockVersion }),
    },
  )
}
