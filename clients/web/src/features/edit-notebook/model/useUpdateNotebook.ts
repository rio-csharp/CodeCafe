import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateNotebook, notesKeys } from '@/entities/notebook'
import type { Notebook, UpdateNotebookRequest } from '@/entities/notebook'

export function useUpdateNotebook(notebookId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: UpdateNotebookRequest) => updateNotebook(notebookId, data),
    onSuccess: (data) => {
      queryClient.setQueryData(notesKeys.detail(data.slug), data)
      // A rename leaves a stale detail cache under the old slug — drop it
      // instead of relying on the broad invalidation below alone.
      queryClient.removeQueries({
        predicate: (query) =>
          query.queryKey[1] === 'detail' &&
          query.queryKey[2] !== data.slug &&
          (query.state.data as Notebook | undefined)?.id === notebookId,
      })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
