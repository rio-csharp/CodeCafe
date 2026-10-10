import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { formatRelativeTime, notebookKeys } from '@/entities/notebook'
import { listTrashedPages, pageKeys, purgeTrashedPage, restoreTrashedPage } from '@/entities/page'
import { Button, ConfirmButton, DialogShell, Spinner } from '@/shared/ui'

export interface PageTrashDialogProps {
  slug: string
  onClose: () => void
}

/**
 * The notebook's page trash: soft-deleted pages wait here for a restore or a
 * purge. Restoring is one click; purging asks twice because there is no undo.
 */
export function PageTrashDialog({ slug, onClose }: PageTrashDialogProps) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()

  const query = useQuery({
    queryKey: pageKeys.trash(slug),
    queryFn: ({ signal }) => listTrashedPages({ slug, signal }),
  })

  const restore = useMutation({
    mutationFn: restoreTrashedPage,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: pageKeys.trash(slug) })
      // The page goes back onto the menu — the tree and the count must follow.
      void queryClient.invalidateQueries({ queryKey: notebookKeys.tree(slug) })
      void queryClient.invalidateQueries({ queryKey: notebookKeys.details(slug) })
    },
  })

  const purge = useMutation({
    mutationFn: purgeTrashedPage,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: pageKeys.trash(slug) })
    },
  })

  const items = query.data?.items ?? []

  return (
    <DialogShell title={t('pageTrash.title')} onClose={onClose} wide>
      {query.isPending ? (
        <div className="grid place-items-center py-10">
          <Spinner />
          <span className="sr-only">{t('list.loading')}</span>
        </div>
      ) : query.isError ? (
        <div className="py-8 text-center">
          <p className="text-sm text-muted">{t('list.loadError')}</p>
          <Button
            variant="ghost"
            size="sm"
            className="mt-3"
            onClick={() => {
              void query.refetch()
            }}
          >
            {t('list.retry')}
          </Button>
        </div>
      ) : items.length === 0 ? (
        <p className="py-8 text-center text-sm text-muted">{t('pageTrash.empty')}</p>
      ) : (
        <ul className="divide-y divide-line">
          {items.map((entry) => (
            <li key={entry.pageId} className="flex items-center gap-3 py-3">
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium text-ink">{entry.title}</p>
                <p className="mt-0.5 text-xs text-muted tabular-nums">
                  {entry.descendantCount > 0
                    ? `${t('pageTrash.descendants', { count: entry.descendantCount })} · `
                    : ''}
                  {t('trash.deletedAt', {
                    time: formatRelativeTime(entry.deletedAtUtc, i18n.language),
                  })}
                </p>
              </div>

              <Button
                variant="ghost"
                size="sm"
                disabled={restore.isPending}
                onClick={() => {
                  restore.mutate(entry.pageId)
                }}
              >
                {t('trash.restore')}
              </Button>

              <ConfirmButton
                label={t('trash.purge')}
                confirmLabel={t('trash.purgeConfirm')}
                busy={purge.isPending}
                onConfirm={() => {
                  purge.mutate(entry.pageId)
                }}
              />
            </li>
          ))}
        </ul>
      )}
    </DialogShell>
  )
}
