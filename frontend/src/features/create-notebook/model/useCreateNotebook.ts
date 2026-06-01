import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createNotebook, notesKeys } from '@/entities/notebook'
import type { CreateNotebookRequest } from '@/entities/notebook'

export function useCreateNotebook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateNotebookRequest) => createNotebook(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
