import { useQuery } from '@tanstack/react-query'
import { getNotebookBySlug, getNotebookItem, getNotebookItems, notesKeys } from '@/entities/notebook'

export function useNotebook(slug: string) {
  return useQuery({
    queryKey: notesKeys.detail(slug),
    queryFn: () => getNotebookBySlug(slug),
  })
}

export function useNotebookItems(
  notebookId: string,
  search?: string,
  includeArchived?: boolean,
  extraEnabled = true,
  includeContent = true,
) {
  return useQuery({
    queryKey: notesKeys.items(notebookId, search, includeArchived, includeContent),
    queryFn: () => getNotebookItems(notebookId, search, includeArchived, includeContent),
    enabled: !!notebookId && extraEnabled,
  })
}

export function useNotebookItem(notebookId: string, itemId: string | null | undefined, enabled = true) {
  return useQuery({
    queryKey: notesKeys.item(notebookId, itemId ?? ''),
    queryFn: () => getNotebookItem(notebookId, itemId!),
    enabled: !!notebookId && !!itemId && enabled,
  })
}
