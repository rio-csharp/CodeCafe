import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createNotebookItem, notesKeys } from '@/entities/notebook'
import type { CreateNotebookItemRequest } from '@/entities/notebook-item'

export function useCreateNotebookItem(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateNotebookItemRequest) => createNotebookItem(notebookId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.items(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
