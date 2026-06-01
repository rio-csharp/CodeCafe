import { useQuery } from '@tanstack/react-query'
import { getNotebookBySlug, getNotebookItems, notesKeys } from '@/entities/notebook'

export function useNotebook(slug: string) {
  return useQuery({
    queryKey: notesKeys.detail(slug),
    queryFn: () => getNotebookBySlug(slug),
  })
}

export function useNotebookItems(notebookId: string, search?: string, includeArchived?: boolean) {
  return useQuery({
    queryKey: notesKeys.items(notebookId, search, includeArchived),
    queryFn: () => getNotebookItems(notebookId, search, includeArchived),
    enabled: !!notebookId,
  })
}
