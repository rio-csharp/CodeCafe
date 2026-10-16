import { apiFetch } from '@/shared/api'
import type { NotebookDetails } from '../model/types'

export interface ImportNotebookData {
  fileName: string
  markdown: string
}

/** Turns a markdown file into a whole notebook; the answer carries its slug. */
export function importNotebook(data: ImportNotebookData): Promise<NotebookDetails> {
  return apiFetch<NotebookDetails>('/api/notebooks/import', {
    method: 'POST',
    body: JSON.stringify(data),
  })
}
