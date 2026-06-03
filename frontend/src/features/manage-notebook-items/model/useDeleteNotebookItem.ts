import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteNotebookItem, notesKeys } from '@/entities/notebook'

export function useDeleteNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => deleteNotebookItem(notebookId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
