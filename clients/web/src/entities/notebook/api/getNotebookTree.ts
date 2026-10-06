import { apiFetch } from '@/shared/api'
import type { NotebookTree } from '../model/types'

export interface GetNotebookTreeParams {
  slug: string
  signal?: AbortSignal
}

/** The whole page hierarchy in one call; archived pages come along flagged. */
export function getNotebookTree({ slug, signal }: GetNotebookTreeParams): Promise<NotebookTree> {
  return apiFetch<NotebookTree>(`/api/notebooks/${encodeURIComponent(slug)}/tree`, { signal })
}
