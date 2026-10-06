import { apiFetch } from '@/shared/api'
import type { NotebookDetails, NotebookVisibility } from '../model/types'

export interface CreateNotebookInput {
  title: string
  description?: string
  visibility: NotebookVisibility
}

/** The server derives the slug from the title when none is requested. */
export function createNotebook(input: CreateNotebookInput): Promise<NotebookDetails> {
  return apiFetch<NotebookDetails>('/api/notebooks', {
    method: 'POST',
    body: JSON.stringify({
      title: input.title,
      description: input.description?.trim() || null,
      slug: null,
      visibility: input.visibility,
    }),
  })
}
