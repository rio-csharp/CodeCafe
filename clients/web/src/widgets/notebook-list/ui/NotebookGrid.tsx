import { useTranslation } from 'react-i18next'
import { NotebookCard } from '@/entities/notebook'
import type { NotebookSummary } from '@/entities/notebook'
import { FavoriteNotebookButton } from '@/features/toggle-notebook-favorite'
import { Button } from '@/shared/ui'

const SKELETON_KEYS = ['a', 'b', 'c', 'd', 'e', 'f'] as const

const GRID_CLASS = 'grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3'

export interface NotebookGridProps {
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
 * A paged grid of notebook cards with its four states. Presentational: the
 * owning page runs the queries and hands the pieces down.
 */
export function NotebookGrid({
  items,
  isPending,
  isError,
  onRetry,
  emptyText,
  hasNextPage = false,
  isFetchingNextPage = false,
  onLoadMore,
  showOwnership = false,
}: NotebookGridProps) {
  const { t } = useTranslation()

  if (isPending) {
    return (
      <ul aria-hidden="true" className={GRID_CLASS}>
        {SKELETON_KEYS.map((key) => (
          <li key={key} className="rounded-xl border border-line bg-card p-5">
            <div className="flex items-start justify-between">
              <div className="size-10 animate-pulse rounded-lg bg-line" />
            </div>
            <div className="mt-3 h-5 w-2/3 animate-pulse rounded bg-line" />
            <div className="mt-2 h-3 w-full animate-pulse rounded bg-line" />
            <div className="mt-1.5 h-3 w-4/5 animate-pulse rounded bg-line" />
            <div className="mt-4 border-t border-line/60 pt-3">
              <div className="ml-auto h-3 w-24 animate-pulse rounded bg-line" />
            </div>
          </li>
        ))}
      </ul>
    )
  }

  if (isError) {
    return (
      <div className="rounded-xl border border-line bg-card py-12 text-center">
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
    return (
      <p className="rounded-xl border border-dashed border-line py-12 text-center text-sm text-muted">
        {emptyText}
      </p>
    )
  }

  return (
    <>
      <ul className={GRID_CLASS}>
        {items.map((notebook) => (
          <NotebookCard
            key={notebook.id}
            notebook={notebook}
            showOwnership={showOwnership}
            cornerAction={
              <FavoriteNotebookButton notebookId={notebook.id} isFavorite={notebook.isFavorite} />
            }
          />
        ))}
      </ul>

      {hasNextPage && onLoadMore !== undefined ? (
        <div className="pt-6 text-center">
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
