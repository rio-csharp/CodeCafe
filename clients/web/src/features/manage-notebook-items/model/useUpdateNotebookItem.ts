import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateNotebookItem, notesKeys } from '@/entities/notebook'
import type { NotebookItem, UpdateNotebookItemRequest } from '@/entities/notebook-item'

export function useUpdateNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ itemId, data }: { itemId: string; data: UpdateNotebookItemRequest }) =>
      updateNotebookItem(notebookId, itemId, data),
    onSuccess: (data) => {
      queryClient.setQueryData<NotebookItem[]>(notesKeys.items(notebookId), (old) => {
        if (!old) return old
        return old.map((item) => (item.id === data.id ? data : item))
      })
      queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
