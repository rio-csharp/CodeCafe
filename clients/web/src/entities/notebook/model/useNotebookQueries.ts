import { useQuery } from '@tanstack/react-query'
import { getNotebookBySlug, getNotebookItem, getNotebookItems, notesKeys } from '@/entities/notebook'

export function useNotebook(slug: string) {
  return useQuery({
    queryKey: notesKeys.detail(slug),
    queryFn: ({ signal }) => getNotebookBySlug(slug, signal),
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
    queryFn: ({ signal }) => getNotebookItems(notebookId, search, includeArchived, includeContent, signal),
    enabled: !!notebookId && extraEnabled,
  })
}

export function useNotebookItem(notebookId: string, itemId: string | null | undefined, enabled = true) {
  return useQuery({
    queryKey: notesKeys.item(notebookId, itemId ?? ''),
    queryFn: ({ signal }) => {
      // Guarded by `enabled` below; throw instead of a non-null assertion so
      // a contract violation fails loudly instead of fetching "undefined".
      if (!itemId) throw new Error('useNotebookItem requires a non-empty itemId')
      return getNotebookItem(notebookId, itemId, signal)
    },
    enabled: !!notebookId && !!itemId && enabled,
  })
}
