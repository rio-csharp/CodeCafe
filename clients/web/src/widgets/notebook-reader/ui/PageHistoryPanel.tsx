import { useState } from 'react'
import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { formatRelativeTime } from '@/entities/notebook'
import { listPageRevisions, pageKeys, restorePageRevision } from '@/entities/page'
import type { PageRevisionGroup, RevisionChangeKind } from '@/entities/page'

export interface PageHistoryPanelProps {
  pageId: string
  /** Writers get the restore button; readers only browse. */
  canWrite: boolean
  /** Called after a successful restore so the reader can refetch the page. */
  onRestored: () => void
}

const KIND_LABEL: Record<RevisionChangeKind, string> = {
  Added: 'history.added',
  Updated: 'history.updated',
  Deleted: 'history.deleted',
  Moved: 'history.moved',
}

/**
 * The page's revision log in the reader's right panel: batches newest-first,
 * each with a change summary. Restoring is a two-click affair — the log is
 * append-only server-side, so a restore can itself be undone by restoring
 * again, but the current unsaved-looking state still gets replaced.
 */
export function PageHistoryPanel({ pageId, canWrite, onRestored }: PageHistoryPanelProps) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [confirmingAt, setConfirmingAt] = useState<string | null>(null)

  const history = useInfiniteQuery({
    queryKey: pageKeys.revisions(pageId),
    queryFn: ({ pageParam, signal }) => listPageRevisions({ pageId, cursor: pageParam, signal }),
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.nextCursor,
  })

  const restore = useMutation({
    mutationFn: (atUtc: string) => restorePageRevision(pageId, atUtc),
    onSuccess: () => {
      setConfirmingAt(null)
      void queryClient.invalidateQueries({ queryKey: pageKeys.revisions(pageId) })
      onRestored()
    },
  })

  const groups = history.data?.pages.flatMap((page) => page.items) ?? []

  const summarize = (group: PageRevisionGroup): string => {
    const counts = new Map<RevisionChangeKind, number>()
    for (const change of group.changes) {
      counts.set(change.changeKind, (counts.get(change.changeKind) ?? 0) + 1)
    }
    return [...counts.entries()]
      .map(([kind, count]) => t(KIND_LABEL[kind], { count }))
      .join(' · ')
  }

  return (
    <div className="flex flex-col gap-2 p-4 text-sm">
      {history.isPending ? (
        <p className="text-xs text-muted">{t('history.loading')}</p>
      ) : history.isError ? (
        <div className="flex items-center gap-2">
          <p className="text-xs text-danger">{t('history.restoreFailed')}</p>
          <button
            type="button"
            onClick={() => {
              void history.refetch()
            }}
            className="text-xs text-accent-strong underline underline-offset-2"
          >
            {t('history.retry')}
          </button>
        </div>
      ) : groups.length === 0 ? (
        <p className="text-xs text-muted">{t('history.empty')}</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {groups.map((group) => (
            <li
              key={group.atUtc}
              className="flex flex-col gap-1.5 rounded-lg border border-line px-3 py-2"
            >
              <div className="flex items-center justify-between gap-2">
                <span className="text-xs font-medium text-ink" title={group.atUtc}>
                  {formatRelativeTime(group.atUtc, i18n.language)}
                </span>
                {group.source === 'Ai' ? (
                  <span className="rounded-full bg-accent-soft px-2 py-0.5 text-[10px] font-medium text-accent-strong">
                    {t('history.aiBadge')}
                  </span>
                ) : null}
              </div>
              <p className="text-xs text-muted">{summarize(group)}</p>
              {canWrite ? (
                <button
                  type="button"
                  disabled={restore.isPending}
                  onClick={() => {
                    if (confirmingAt === group.atUtc) {
                      restore.mutate(group.atUtc)
                    } else {
                      setConfirmingAt(group.atUtc)
                    }
                  }}
                  className={`self-start rounded-md px-2 py-1 text-xs font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50 ${
                    confirmingAt === group.atUtc
                      ? 'bg-danger-soft text-danger'
                      : 'text-accent-strong hover:bg-muted-soft'
                  }`}
                >
                  {restore.isPending && confirmingAt === group.atUtc
                    ? t('history.restoring')
                    : confirmingAt === group.atUtc
                      ? t('history.restoreConfirm')
                      : t('history.restore')}
                </button>
              ) : null}
            </li>
          ))}
        </ul>
      )}

      {restore.isError ? (
        <p role="alert" className="text-xs text-danger">
          {t('history.restoreFailed')}
        </p>
      ) : null}

      {history.hasNextPage ? (
        <button
          type="button"
          disabled={history.isFetchingNextPage}
          onClick={() => {
            void history.fetchNextPage()
          }}
          className="self-start rounded-md px-2 py-1 text-xs font-medium text-accent-strong transition-colors hover:bg-muted-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
        >
          {history.isFetchingNextPage ? t('history.loading') : t('history.loadMore')}
        </button>
      ) : null}
    </div>
  )
}
