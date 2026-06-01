import { useMutation, useQueryClient } from '@tanstack/react-query'
import { reorderNotebookItems, notesKeys } from '@/entities/notebook'
import type { ReorderItemsPayload } from '@/entities/notebook-item'

export function useReorderNotebookItems(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: ReorderItemsPayload) => reorderNotebookItems(notebookId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
