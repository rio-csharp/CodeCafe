import { useMutation, useQueryClient, type Query } from '@tanstack/react-query'
import { deleteNotebookItem, notesKeys } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'

export function useDeleteNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => deleteNotebookItem(notebookId, itemId),
    onSuccess: (_data, itemId) => {
      const itemsRootKey = notesKeys.itemsRoot(notebookId)
      const cachedLists = queryClient
        .getQueriesData<NotebookItem[]>({ queryKey: itemsRootKey })
        .map(([, data]) => data)
        .filter((data): data is NotebookItem[] => Array.isArray(data))

      // A folder delete also removes all descendants on the server. Remove the
      // same subtree from every cached list variant immediately so the UI does
      // not keep showing successfully deleted items while the refetch runs.
      const deletedIds = new Set([itemId])
      let foundDescendant = true
      while (foundDescendant) {
        foundDescendant = false
        for (const items of cachedLists) {
          for (const item of items) {
            if (item.parentId && deletedIds.has(item.parentId) && !deletedIds.has(item.id)) {
              deletedIds.add(item.id)
              foundDescendant = true
            }
          }
        }
      }

      queryClient.setQueriesData<NotebookItem[]>(
        {
          queryKey: itemsRootKey,
          predicate: (query: Query) => Array.isArray(query.state.data),
        },
        (old) => old?.filter((item) => !deletedIds.has(item.id)),
      )
      queryClient.invalidateQueries({ queryKey: itemsRootKey })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
