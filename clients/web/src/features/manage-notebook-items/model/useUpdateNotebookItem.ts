import { useMutation, useQueryClient, type Query } from '@tanstack/react-query'
import { updateNotebookItem, notesKeys } from '@/entities/notebook'
import type { NotebookItem, UpdateNotebookItemRequest } from '@/entities/notebook-item'

export function useUpdateNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ itemId, data }: { itemId: string; data: UpdateNotebookItemRequest }) =>
      updateNotebookItem(notebookId, itemId, data),
    onSuccess: (data) => {
      // The items cache key includes search/archive/content params, so a
      // fixed-key setQueryData never matches; update every cached list variant.
      queryClient.setQueriesData<NotebookItem[]>(
        {
          queryKey: notesKeys.itemsRoot(notebookId),
          predicate: (query: Query) => Array.isArray(query.state.data),
        },
        (old) => {
          if (!old) return old
          return old.map((item) => (item.id === data.id ? data : item))
        },
      )
      queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
