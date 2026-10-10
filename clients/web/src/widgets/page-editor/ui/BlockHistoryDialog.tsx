import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { formatRelativeTime } from '@/entities/notebook'
import { listBlockRevisions, pageKeys, restoreBlockRevision } from '@/entities/page'
import type { RevisionChangeKind } from '@/entities/page'
import { DialogShell } from '@/shared/ui'

export interface BlockHistoryDialogProps {
  pageId: string
  blockId: string
  onClose: () => void
  /**
   * Called after a successful restore. The editor's open draft is stale at
   * that point, so the parent is expected to leave edit mode and refetch.
   */
  onRestored: () => void
}

const KIND_LABEL: Record<RevisionChangeKind, string> = {
  Added: 'blockHistory.kind.Added',
  Updated: 'blockHistory.kind.Updated',
  Deleted: 'blockHistory.kind.Deleted',
  Moved: 'blockHistory.kind.Moved',
}

/**
 * One block's revision log, newest-first, with a per-row restore. Mirrors the
 * reader's PageHistoryPanel; unlike it, this opens on top of the editor, so
 * the restore contract (exit edit mode, refetch) is the caller's job.
 */
export function BlockHistoryDialog({ pageId, blockId, onClose, onRestored }: BlockHistoryDialogProps) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const [restoreFailed, setRestoreFailed] = useState(false)

  const history = useInfiniteQuery({
    queryKey: pageKeys.blockRevisions(pageId, blockId),
    queryFn: ({ pageParam, signal }) =>
      listBlockRevisions({ pageId, blockId, cursor: pageParam, signal }),
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.nextCursor,
  })

  const restore = useMutation({
    mutationFn: (blockVersion: number) => restoreBlockRevision(pageId, blockId, blockVersion),
    onSuccess: () => {
      // The restored content only reaches the reader through a fresh fetch.
      void queryClient.invalidateQueries({ queryKey: pageKeys.all })
      onRestored()
    },
    onError: () => {
      setRestoreFailed(true)
    },
  })

  const revisions = history.data?.pages.flatMap((page) => page.items) ?? []

  return (
    <DialogShell title={t('blockHistory.title')} onClose={onClose}>
      {history.isPending ? (
        <p className="text-xs text-muted">{t('history.loading')}</p>
      ) : history.isError ? (
        <div className="flex items-center gap-2">
          <p className="text-xs text-danger">{t('history.loadFailed')}</p>
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
      ) : revisions.length === 0 ? (
        <p className="text-xs text-muted">{t('blockHistory.empty')}</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {revisions.map((revision) => (
            <li
              key={`${revision.blockVersion}-${revision.createdAtUtc}`}
              className="flex items-center gap-2 rounded-lg border border-line px-3 py-2"
            >
              <span
                className="shrink-0 text-xs font-medium text-ink"
                title={revision.createdAtUtc}
              >
                {formatRelativeTime(revision.createdAtUtc, i18n.language)}
              </span>
              <span className="min-w-0 flex-1 truncate text-xs text-muted">
                {t(KIND_LABEL[revision.changeKind])}
              </span>
              {revision.source === 'Ai' ? (
                <span className="shrink-0 rounded-full bg-accent-soft px-2 py-0.5 text-[10px] font-medium text-accent-strong">
                  {t('history.aiBadge')}
                </span>
              ) : null}
              <button
                type="button"
                disabled={restore.isPending}
                onClick={() => {
                  setRestoreFailed(false)
                  restore.mutate(revision.blockVersion)
                }}
                className="shrink-0 rounded-md px-2 py-1 text-xs font-medium text-accent-strong transition-colors hover:bg-muted-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
              >
                {restore.isPending ? t('blockHistory.restoring') : t('blockHistory.restore')}
              </button>
            </li>
          ))}
        </ul>
      )}

      {history.hasNextPage ? (
        <button
          type="button"
          disabled={history.isFetchingNextPage}
          onClick={() => {
            void history.fetchNextPage()
          }}
          className="mt-2 self-start rounded-md px-2 py-1 text-xs font-medium text-accent-strong transition-colors hover:bg-muted-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
        >
          {history.isFetchingNextPage ? t('history.loading') : t('history.loadMore')}
        </button>
      ) : null}

      {restoreFailed ? (
        <p role="alert" className="mt-3 text-xs text-danger">
          {t('blockHistory.restoreFailed')}
        </p>
      ) : null}
    </DialogShell>
  )
}
