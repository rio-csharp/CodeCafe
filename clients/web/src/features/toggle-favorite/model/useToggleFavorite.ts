import { useMutation, useQueryClient, type Query } from '@tanstack/react-query'
import { addFavorite, removeFavorite, notesKeys, type Notebook } from '@/entities/notebook'

function isNotebookListKey(query: Query): boolean {
  const key = query.queryKey
  return (
    Array.isArray(key) &&
    key[0] === 'notes' &&
    (key[1] === 'public' || key[1] === 'mine') &&
    Array.isArray(query.state.data)
  )
}

function toggleInList(notebooks: Notebook[] | undefined, notebookId: string): Notebook[] | undefined {
  if (!notebooks) return notebooks
  return notebooks.map((notebook) =>
    notebook.id === notebookId
      ? {
          ...notebook,
          isFavoritedByMe: !notebook.isFavoritedByMe,
          favoriteCount: notebook.favoriteCount + (notebook.isFavoritedByMe ? -1 : 1),
        }
      : notebook,
  )
}

export function useToggleFavorite() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ notebookId, isFavorited }: { notebookId: string; isFavorited: boolean }) => {
      const result = isFavorited
        ? await removeFavorite(notebookId)
        : await addFavorite(notebookId)
      return result
    },
    onMutate: async ({ notebookId }) => {
      await queryClient.cancelQueries({ queryKey: notesKeys.all })
      const snapshots = queryClient.getQueriesData<Notebook[]>({ queryKey: notesKeys.all })
      // Optimistic toggle so the star responds instantly on slow networks.
      queryClient.setQueriesData<Notebook[]>(
        { queryKey: notesKeys.all, predicate: isNotebookListKey },
        (old) => toggleInList(old, notebookId),
      )
      return { snapshots }
    },
    onError: (_err, _vars, context) => {
      // Roll back every touched cache to its pre-mutation value.
      context?.snapshots.forEach(([key, data]) => {
        queryClient.setQueryData(key, data)
      })
    },
    onSettled: (_result, _error, { notebookId }) => {
      queryClient.invalidateQueries({ queryKey: notesKeys.favorite(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
