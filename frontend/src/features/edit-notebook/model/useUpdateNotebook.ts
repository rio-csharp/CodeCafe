import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateNotebook, notesKeys } from '@/entities/notebook'
import type { UpdateNotebookRequest } from '@/entities/notebook'

export function useUpdateNotebook(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateNotebookRequest) => updateNotebook(notebookId, data),
    onSuccess: (data) => {
      queryClient.setQueryData(notesKeys.detail(data.slug), data)
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
