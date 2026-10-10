import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { listFavoritePages, pageKeys } from '@/entities/page'
import { useSessionStore } from '@/entities/session'
import { normalizePagePath, toPageHref } from '../lib/tree'

export interface FavoritePagesSectionProps {
  /** Narrows the favorites to the notebook being read. */
  notebookId: string
  /** The open page's path, for the active highlight. */
  activePath?: string | null
  /** Lets the mobile drawer close itself once a page is chosen. */
  onNavigate?: () => void
}

/**
 * The signed-in reader's starred pages of this notebook, above the tree.
 * Anonymous readers, empty lists and load hiccups all render nothing — the
 * section is a shortcut, never an obstacle.
 */
export function FavoritePagesSection({ notebookId, activePath = null, onNavigate }: FavoritePagesSectionProps) {
  const { t } = useTranslation()
  const status = useSessionStore((state) => state.status)

  const favorites = useQuery({
    queryKey: pageKeys.favorites(notebookId),
    queryFn: ({ signal }) => listFavoritePages({ notebookId, signal }),
    enabled: status === 'authenticated',
  })

  const entries = favorites.data
  if (status !== 'authenticated' || entries === undefined || entries.length === 0) {
    return null
  }

  const active = activePath === null ? null : normalizePagePath(activePath)

  return (
    <section aria-label={t('reader.favoritePages')} className="mb-3">
      <h3 className="mb-1 flex items-center gap-1.5 px-2 text-[11px] font-semibold tracking-wide text-muted uppercase">
        <svg
          viewBox="0 0 16 16"
          className="size-3"
          fill="currentColor"
          stroke="currentColor"
          strokeWidth="1"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M8 1.6 9.9 5.5l4.3.6-3.1 3 .7 4.3L8 11.5l-3.8 2 .7-4.3-3.1-3 4.3-.6Z" />
        </svg>
        {t('reader.favoritePages')}
      </h3>
      <ul className="flex flex-col gap-1">
        {entries.map((entry) => {
          const isActive = active !== null && normalizePagePath(entry.path) === active
          return (
            <li key={entry.pageId}>
              <Link
                to={toPageHref(entry.notebookSlug, entry.path)}
                aria-current={isActive ? 'page' : undefined}
                onClick={onNavigate}
                className={[
                  'block truncate rounded px-2 py-1 text-sm transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
                  isActive
                    ? 'bg-accent-soft font-medium text-accent-strong'
                    : 'text-ink hover:bg-muted-soft hover:text-accent-strong',
                ].join(' ')}
              >
                {entry.title}
              </Link>
            </li>
          )
        })}
      </ul>
    </section>
  )
}
