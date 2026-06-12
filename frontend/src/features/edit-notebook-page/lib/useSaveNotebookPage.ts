import { useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { useMutation, useQueryClient, type Query } from '@tanstack/react-query'
import { updateNotebookItem, notesKeys } from '@/entities/notebook'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import type { NotebookItem } from '@/entities/notebook-item'

interface UseSaveNotebookPageOptions {
  onSuccess?: () => void
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
    mutationFn: ({ itemId, data }: { itemId: string; data: { title: string; sortOrder: number; contentJson: Record<string, unknown> } }) =>
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
      updateItem.mutate(
        {
          itemId: activePage.id,
          data: {
            title: activePage.title,
            sortOrder: activePage.sortOrder,
            contentJson,
          },
        },
        {
          onSuccess: () => {
            showToast(t('notebook.saved'))
            onSuccess?.()
          },
          onError: (err: unknown) => {
            showToast(getErrorMessage(err, t('notebook.saveFailed')), 'error')
          },
        },
      )
    },
    [activePage, updateItem, showToast, onSuccess, t],
  )

  return { handleSave, isPending: updateItem.isPending }
}
