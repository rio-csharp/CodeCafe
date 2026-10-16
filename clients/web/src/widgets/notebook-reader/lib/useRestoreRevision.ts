import { useMutation, useQueryClient } from '@tanstack/react-query'
import { pageKeys, restorePageRevision } from '@/entities/page'

/** Restore mutation shared by the history list and the preview dialog. */
export function useRestoreRevision(pageId: string, onRestored: () => void) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (atUtc: string) => restorePageRevision(pageId, atUtc),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: pageKeys.revisions(pageId) })
      onRestored()
    },
  })
}
