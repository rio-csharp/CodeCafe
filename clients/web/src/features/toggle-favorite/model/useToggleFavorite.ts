import { useMutation, useQueryClient, type InfiniteData, type Query } from '@tanstack/react-query'
import { addFavorite, removeFavorite, notesKeys, type Notebook } from '@/entities/notebook'
import { applyFavoriteToNotebookLists } from './applyFavoriteToNotebookLists'

// Notebook lists are cached as useInfiniteQuery data ({ pages, pageParams });
// other notes keys hold details, item arrays, or favorite status objects.
// The key check keeps future infinite queries under notesKeys (e.g. items)
// from having notebook fields grafted on just because their data has pages.
function isNotebookListQuery(query: Query): boolean {
  const key = query.queryKey
  const isNotebookListKey =
    key[0] === notesKeys.all[0] && (key[1] === notesKeys.public()[1] || key[1] === notesKeys.mine()[1])
  if (!isNotebookListKey) return false
  const data = query.state.data as InfiniteData<Notebook[]> | undefined
  return Array.isArray(data?.pages)
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
      const snapshots = queryClient.getQueriesData<InfiniteData<Notebook[]>>({ queryKey: notesKeys.all })
      // Optimistic toggle so the star responds instantly on slow networks.
      queryClient.setQueriesData<InfiniteData<Notebook[]>>(
        { queryKey: notesKeys.all, predicate: isNotebookListQuery },
        (old) => applyFavoriteToNotebookLists(old, notebookId),
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
