import { useMutation, useQueryClient } from '@tanstack/react-query'
import { addFavorite, removeFavorite, notesKeys } from '@/entities/notebook'

export function useToggleFavorite() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ notebookId, isFavorited }: { notebookId: string; isFavorited: boolean }) => {
      const result = isFavorited
        ? await removeFavorite(notebookId)
        : await addFavorite(notebookId)
      return result
    },
    onSuccess: (_, { notebookId }) => {
      queryClient.invalidateQueries({ queryKey: notesKeys.favorite(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
