import { useTranslation } from 'react-i18next'
import { NotebookRow } from '@/entities/notebook'
import type { NotebookSummary } from '@/entities/notebook'
import { Button } from '@/shared/ui'

const SKELETON_KEYS = ['a', 'b', 'c', 'd', 'e'] as const

export interface NotebookRowsProps {
  items: readonly NotebookSummary[]
  isPending: boolean
  isError: boolean
  onRetry?: () => void
  emptyText: string
  hasNextPage?: boolean
  isFetchingNextPage?: boolean
  onLoadMore?: () => void
  /** Shows visibility badges and favorite stars — pass true on "my notebooks". */
  showOwnership?: boolean
}

/**
 * A paged list of notebook rows with its four states. Presentational: the
 * owning page runs the queries and hands the pieces down.
 */
export function NotebookRows({
  items,
  isPending,
  isError,
  onRetry,
  emptyText,
  hasNextPage = false,
  isFetchingNextPage = false,
  onLoadMore,
  showOwnership = false,
}: NotebookRowsProps) {
  const { t } = useTranslation()

  if (isPending) {
    return (
      <ul aria-hidden="true" className="divide-y divide-line">
        {SKELETON_KEYS.map((key) => (
          <li key={key} className="flex items-center gap-4 py-3.5">
            <div className="min-w-0 flex-1">
              <div className="h-4 w-2/5 animate-pulse rounded bg-line" />
              <div className="mt-1.5 h-3 w-3/5 animate-pulse rounded bg-line" />
            </div>
            <div className="h-3 w-20 shrink-0 animate-pulse rounded bg-line" />
          </li>
        ))}
      </ul>
    )
  }

  if (isError) {
    return (
      <div className="py-8 text-center">
        <p className="text-sm text-muted">{t('list.loadError')}</p>
        {onRetry !== undefined ? (
          <Button variant="ghost" className="mt-3" onClick={onRetry}>
            {t('list.retry')}
          </Button>
        ) : null}
      </div>
    )
  }

  if (items.length === 0) {
    return <p className="py-8 text-center text-sm text-muted">{emptyText}</p>
  }

  return (
    <>
      <ul className="divide-y divide-line">
        {items.map((notebook) => (
          <NotebookRow key={notebook.id} notebook={notebook} showOwnership={showOwnership} />
        ))}
      </ul>

      {hasNextPage && onLoadMore !== undefined ? (
        <div className="pt-4 text-center">
          <Button
            variant="ghost"
            disabled={isFetchingNextPage}
            onClick={() => {
              onLoadMore()
            }}
          >
            {isFetchingNextPage ? t('list.loading') : t('list.loadMore')}
          </Button>
        </div>
      ) : null}
    </>
  )
}
