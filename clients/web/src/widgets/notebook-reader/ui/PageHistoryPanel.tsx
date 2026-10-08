import { useState } from 'react'
import { useInfiniteQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import type { BlockDto } from '@/entities/block'
import { formatRelativeTime } from '@/entities/notebook'
import { listPageRevisions, pageKeys } from '@/entities/page'
import type { PageRevisionGroup, RevisionChangeKind } from '@/entities/page'
import { RevisionPreviewDialog } from './RevisionPreviewDialog'

export interface PageHistoryPanelProps {
  pageId: string
  /** The live blocks; the preview dialog diffs against them. */
  currentBlocks: readonly BlockDto[]
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
 * The page's revision log in the reader's right panel: one line per batch
 * (time + change summary), newest-first. Clicking a batch opens the preview
 * dialog — historical content, a diff against now, and the restore button.
 */
export function PageHistoryPanel({ pageId, currentBlocks, canWrite, onRestored }: PageHistoryPanelProps) {
  const { t, i18n } = useTranslation()
  const [viewingAt, setViewingAt] = useState<string | null>(null)

  const history = useInfiniteQuery({
    queryKey: pageKeys.revisions(pageId),
    queryFn: ({ pageParam, signal }) => listPageRevisions({ pageId, cursor: pageParam, signal }),
    initialPageParam: null as string | null,
    getNextPageParam: (last) => last.nextCursor,
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
      ) : groups.length === 0 ? (
        <p className="text-xs text-muted">{t('history.empty')}</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {groups.map((group) => (
            <li key={group.atUtc}>
              {/* One line per batch: time and summary, click anywhere to preview. */}
              <button
                type="button"
                onClick={() => {
                  setViewingAt(group.atUtc)
                }}
                aria-label={t('history.view')}
                className="flex w-full items-center gap-2 rounded-lg border border-line px-3 py-2 text-left transition-colors hover:bg-muted-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
              >
                <span
                  className="shrink-0 text-xs font-medium text-ink"
                  title={group.atUtc}
                >
                  {formatRelativeTime(group.atUtc, i18n.language)}
                </span>
                <span className="min-w-0 flex-1 truncate text-xs text-muted">
                  {summarize(group)}
                </span>
                {group.source === 'Ai' ? (
                  <span className="shrink-0 rounded-full bg-accent-soft px-2 py-0.5 text-[10px] font-medium text-accent-strong">
                    {t('history.aiBadge')}
                  </span>
                ) : null}
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
          className="self-start rounded-md px-2 py-1 text-xs font-medium text-accent-strong transition-colors hover:bg-muted-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
        >
          {history.isFetchingNextPage ? t('history.loading') : t('history.loadMore')}
        </button>
      ) : null}

      {viewingAt !== null ? (
        <RevisionPreviewDialog
          pageId={pageId}
          atUtc={viewingAt}
          currentBlocks={currentBlocks}
          canWrite={canWrite}
          onRestored={onRestored}
          onClose={() => {
            setViewingAt(null)
          }}
        />
      ) : null}
    </div>
  )
}
