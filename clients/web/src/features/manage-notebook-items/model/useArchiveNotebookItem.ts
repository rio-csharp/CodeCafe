import { useMutation, useQueryClient } from '@tanstack/react-query'
import { archiveNotebookItem, notesKeys } from '@/entities/notebook'

export function useArchiveNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (itemId: string) => archiveNotebookItem(notebookId, itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
