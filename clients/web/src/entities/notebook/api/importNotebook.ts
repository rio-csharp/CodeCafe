import { apiFetch } from '@/shared/api'
import type { NotebookDetails } from '../model/types'

// A whole notebook can be several megabytes on a slow uplink; the default 30s budget is for
// ordinary JSON calls.
const IMPORT_TIMEOUT_MS = 120_000

export interface ImportNotebookData {
  fileName: string
  markdown: string
}

/** Turns a markdown file into a whole notebook; the answer carries its slug. */
export function importNotebook(data: ImportNotebookData): Promise<NotebookDetails> {
  return apiFetch<NotebookDetails>('/api/notebooks/import', {
    method: 'POST',
    timeoutMs: IMPORT_TIMEOUT_MS,
    body: JSON.stringify(data),
  })
}
