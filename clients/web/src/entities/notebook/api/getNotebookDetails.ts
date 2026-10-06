import { apiFetch } from '@/shared/api'
import type { NotebookDetails } from '../model/types'

export interface GetNotebookDetailsParams {
  /** Slug or id — the details endpoint accepts either. */
  slug: string
  signal?: AbortSignal
}

/** Anonymous-readable: a private notebook answers 404 rather than 403. */
export function getNotebookDetails({
  slug,
  signal,
}: GetNotebookDetailsParams): Promise<NotebookDetails> {
  return apiFetch<NotebookDetails>(`/api/notebooks/${encodeURIComponent(slug)}`, { signal })
}
