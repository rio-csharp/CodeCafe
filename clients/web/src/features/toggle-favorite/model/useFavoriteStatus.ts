import { useQuery } from '@tanstack/react-query'
import { getFavoriteStatus, notesKeys } from '@/entities/notebook'

export function useFavoriteStatus(notebookId: string) {
  return useQuery({
    queryKey: notesKeys.favorite(notebookId),
    queryFn: () => getFavoriteStatus(notebookId),
    enabled: !!notebookId,
  })
}
