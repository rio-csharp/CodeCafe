import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteNotebook, notesKeys } from '@/entities/notebook'

export function useDeleteNotebook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (notebookId: string) => deleteNotebook(notebookId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
