import { useMutation, useQueryClient, type InfiniteData } from '@tanstack/react-query'
import { createNotebook, notesKeys } from '@/entities/notebook'
import type { CreateNotebookRequest, Notebook } from '@/entities/notebook'

export function useCreateNotebook() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateNotebookRequest) => createNotebook(data),
    onSuccess: (createdNotebook) => {
      // Keep the first page responsive while the invalidated server query
      // refetches. This also makes a newly created notebook visible when the
      // user returns from the editor immediately after creation.
      // mine() with no search term IS the unfiltered list key, so prefix
      // matching on it targets exactly that variant — filtered lists encode
      // the term at key[2] and a new notebook may not match an active filter.
      queryClient.setQueriesData<InfiniteData<Notebook[]>>(
        { queryKey: notesKeys.mine() },
        (old) => {
          if (!old || old.pages.length === 0) return old
          const firstPage = old.pages[0]
          if (firstPage.some((notebook) => notebook.id === createdNotebook.id)) return old
          return { ...old, pages: [[createdNotebook, ...firstPage], ...old.pages.slice(1)] }
        },
      )
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}
