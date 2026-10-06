import { apiFetch } from '@/shared/api'
import type { NotebookDetails, NotebookVisibility } from '../model/types'

export interface CreateNotebookInput {
  title: string
  description?: string
  visibility: NotebookVisibility
  /** Custom URL slug; the server derives one from the title when omitted. */
  slug?: string
}

export function createNotebook(input: CreateNotebookInput): Promise<NotebookDetails> {
  return apiFetch<NotebookDetails>('/api/notebooks', {
    method: 'POST',
    body: JSON.stringify({
      title: input.title,
      description: input.description?.trim() || null,
      slug: input.slug?.trim() || null,
      visibility: input.visibility,
    }),
  })
}
