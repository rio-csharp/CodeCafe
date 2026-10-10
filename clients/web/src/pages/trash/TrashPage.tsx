import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link, Navigate } from 'react-router'
import {
  emptyTrash,
  formatRelativeTime,
  listTrash,
  notebookKeys,
  purgeTrashedNotebook,
  restoreTrashedNotebook,
} from '@/entities/notebook'
import { useSessionStore } from '@/entities/session'
import { Button, ConfirmButton, Container, Spinner } from '@/shared/ui'

/**
 * The trash can: soft-deleted notebooks wait here for a restore or a purge.
 * Restoring is one click; purging asks twice because there is no undo.
 */
export function TrashPage() {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const status = useSessionStore((state) => state.status)

  const query = useQuery({
    queryKey: notebookKeys.trash(),
    queryFn: ({ signal }) => listTrash(signal),
    enabled: status === 'authenticated',
  })

  const invalidateAll = () => {
    void queryClient.invalidateQueries({ queryKey: notebookKeys.all })
  }

  const restore = useMutation({
    mutationFn: restoreTrashedNotebook,
    onSuccess: invalidateAll,
  })
  const purge = useMutation({
    mutationFn: purgeTrashedNotebook,
    onSuccess: invalidateAll,
  })
  const empty = useMutation({
    mutationFn: emptyTrash,
    onSuccess: invalidateAll,
  })

  if (status === 'anonymous') {
    return <Navigate to="/" replace />
  }

  const items = query.data?.items ?? []

  return (
    <div className="min-h-dvh bg-canvas">
      <Container width="narrow" className="py-12 sm:py-16">
        <Link
          to="/"
          className="inline-flex items-center gap-1.5 text-sm text-muted transition-colors hover:text-ink"
        >
          <svg
            viewBox="0 0 16 16"
            className="size-3.5"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.6"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <path d="M9.5 3.5 5 8l4.5 4.5" />
          </svg>
          {t('trash.backHome')}
        </Link>

        <div className="mt-6 flex flex-wrap items-center justify-between gap-3 border-b border-line pb-4">
          <h1 className="text-xl font-semibold text-ink">{t('trash.title')}</h1>
          {items.length > 0 ? (
            <ConfirmButton
              label={t('trash.emptyAll')}
              confirmLabel={t('trash.emptyAllConfirm')}
              onConfirm={() => {
                empty.mutate()
              }}
              busy={empty.isPending}
            />
          ) : null}
        </div>

        {status !== 'authenticated' || query.isPending ? (
          <div className="grid place-items-center py-16">
            <Spinner />
            <span className="sr-only">{t('list.loading')}</span>
          </div>
        ) : query.isError ? (
          <div className="py-12 text-center">
            <p className="text-sm text-muted">{t('list.loadError')}</p>
            <Button
              variant="ghost"
              className="mt-3"
              onClick={() => {
                void query.refetch()
              }}
            >
              {t('list.retry')}
            </Button>
          </div>
        ) : items.length === 0 ? (
          <p className="py-12 text-center text-sm text-muted">{t('trash.empty')}</p>
        ) : (
          <ul className="divide-y divide-line">
            {items.map((entry) => (
              <li key={entry.notebookId} className="flex items-center gap-4 py-3.5">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-ink">{entry.title}</p>
                  <p className="mt-0.5 text-xs text-muted tabular-nums">
                    {t('list.pageCount', { count: entry.pageCount })}
                    {' · '}
                    {t('trash.deletedAt', {
                      time: formatRelativeTime(entry.deletedAtUtc, i18n.language),
                    })}
                  </p>
                </div>

                <Button
                  variant="ghost"
                  disabled={restore.isPending}
                  onClick={() => {
                    restore.mutate(entry.notebookId)
                  }}
                >
                  {t('trash.restore')}
                </Button>

                <ConfirmButton
                  label={t('trash.purge')}
                  confirmLabel={t('trash.purgeConfirm')}
                  onConfirm={() => {
                    purge.mutate(entry.notebookId)
                  }}
                  busy={purge.isPending}
                />
              </li>
            ))}
          </ul>
        )}
      </Container>
    </div>
  )
}
