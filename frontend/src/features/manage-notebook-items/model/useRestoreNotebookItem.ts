import { useMutation, useQueryClient } from '@tanstack/react-query'
import { restoreNotebookItem, notesKeys } from '@/entities/notebook'

export function useRestoreNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => restoreNotebookItem(notebookId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
