import { apiFetch } from '@/shared/api'

export interface NotebookSlugAvailability {
  slug: string
  isAvailable: boolean
  /** Free alternatives the server offers when the slug is taken. */
  suggestions: string[]
}

/** Advisory check; the create call stays the authoritative answer. */
export function getNotebookSlugAvailability(
  slug: string,
  signal?: AbortSignal,
): Promise<NotebookSlugAvailability> {
  return apiFetch<NotebookSlugAvailability>(
    `/api/notebooks/slugs/${encodeURIComponent(slug)}`,
    { signal },
  )
}
