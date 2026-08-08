import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient, type Query, type QueryClient } from '@tanstack/react-query'
import { updateNotebookItem, notesKeys } from '@/entities/notebook'
import { useToast } from '@/shared/ui/Toast'
import { ApiError } from '@/shared/api'
import { getErrorMessage } from '@/shared/lib'
import type { NotebookItem, UpdateNotebookItemRequest } from '@/entities/notebook-item'

interface UseSaveNotebookPageOptions {
  onSuccess?: () => void
}

/**
 * Latest cached version of the item across the items-list queries, so a
 * rename/reorder done in the tree while the editor was open isn't
 * overwritten with the title/sortOrder captured when the editor opened.
 */
function findCachedItem(
  queryClient: QueryClient,
  notebookId: string,
  itemId: string,
): NotebookItem | null {
  for (const [, data] of queryClient.getQueriesData<NotebookItem[]>({
    queryKey: notesKeys.itemsRoot(notebookId),
  })) {
    if (!Array.isArray(data)) continue
    const found = data.find((item) => item.id === itemId)
    if (found) return found
  }
  return null
}

export function useSaveNotebookPage(
  notebookId: string,
  activePage: NotebookItem | null,
  options?: UseSaveNotebookPageOptions,
) {
  const { t } = useTranslation()
  const { showToast } = useToast()
  const queryClient = useQueryClient()
  const { onSuccess } = options ?? {}

  const updateItem = useMutation({
    mutationFn: ({ itemId, data }: { itemId: string; data: UpdateNotebookItemRequest }) =>
      updateNotebookItem(notebookId, itemId, data),
    onSuccess: (data) => {
      queryClient.setQueriesData<NotebookItem[]>(
        {
          queryKey: notesKeys.itemsRoot(notebookId),
          predicate: (query: Query) => Array.isArray(query.state.data),
        },
        (old) => {
          if (!old) return old
          return old.map((item) => (item.id === data.id ? { ...item, title: data.title, sortOrder: data.sortOrder } : item))
        },
      )
      queryClient.invalidateQueries({ queryKey: notesKeys.item(notebookId, data.id) })
      queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })

  const handleSave = useCallback(
    (contentJson: Record<string, unknown>) => {
      if (!activePage) return
      const latest = findCachedItem(queryClient, notebookId, activePage.id) ?? activePage
      updateItem.mutate(
        {
          itemId: activePage.id,
          data: {
            title: latest.title,
            sortOrder: latest.sortOrder,
            contentJson,
            expectedUpdatedAtUtc: activePage.updatedAtUtc,
          },
        },
        {
          onSuccess: () => {
            showToast(t('notebook.saved'))
            onSuccess?.()
          },
          onError: (err: unknown) => {
            // Optimistic-concurrency conflict: the page changed since the
            // editor was opened — pull the fresh version so the user can
            // reconcile instead of silently overwriting it.
            if (err instanceof ApiError && err.status === 409) {
              queryClient.invalidateQueries({ queryKey: notesKeys.item(notebookId, activePage.id) })
              queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
            }
            showToast(getErrorMessage(err, t('notebook.saveFailed')), 'error')
          },
        },
      )
    },
    [activePage, notebookId, queryClient, updateItem, showToast, onSuccess, t],
  )

  return { handleSave, isPending: updateItem.isPending }
}
