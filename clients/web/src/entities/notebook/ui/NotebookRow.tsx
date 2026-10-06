import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { NotebookSummary } from '../model/types'
import { formatRelativeTime } from '../lib/formatRelativeTime'

export interface NotebookRowProps {
  notebook: NotebookSummary
  /** Ownership cues (visibility badge, favorite star) — only meaningful on "mine". */
  showOwnership?: boolean
}

/**
 * One notebook as a list row, not a card: the title is the link, the meta sits
 * right-aligned, and hairlines between rows do the separating.
 */
export function NotebookRow({ notebook, showOwnership = false }: NotebookRowProps) {
  const { t, i18n } = useTranslation()

  return (
    <li>
      <Link
        to={`/notebooks/${encodeURIComponent(notebook.slug)}`}
        className="group -mx-2 flex items-center gap-4 rounded-md px-2 py-3.5 transition-colors hover:bg-card focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="truncate font-medium text-ink transition-colors group-hover:text-accent-strong">
              {notebook.title}
            </span>

            {showOwnership && notebook.visibility !== 'Private' ? (
              <span className="shrink-0 rounded border border-line px-1.5 py-px text-[11px] leading-4 text-muted">
                {t(`visibility.${notebook.visibility}`)}
              </span>
            ) : null}

            {showOwnership && notebook.isFavorite ? (
              <svg
                viewBox="0 0 16 16"
                className="size-3.5 shrink-0 fill-accent text-accent"
                aria-label={t('home.filterFavorites')}
                role="img"
              >
                <path d="M8 1.6 9.9 5.5l4.3.6-3.1 3 .7 4.3L8 11.5l-3.8 2 .7-4.3-3.1-3 4.3-.6Z" />
              </svg>
            ) : null}
          </div>

          {notebook.description !== null && notebook.description.length > 0 ? (
            <p className="mt-0.5 truncate text-sm text-muted">{notebook.description}</p>
          ) : null}
        </div>

        {notebook.tags.length > 0 ? (
          <div className="hidden shrink-0 gap-2 text-xs text-muted md:flex">
            {notebook.tags.slice(0, 3).map((tag) => (
              <span key={tag}>#{tag}</span>
            ))}
          </div>
        ) : null}

        <div className="shrink-0 text-xs whitespace-nowrap text-muted tabular-nums">
          {t('list.pageCount', { count: notebook.pageCount })}
          {' · '}
          {formatRelativeTime(notebook.updatedAtUtc, i18n.language)}
        </div>
      </Link>
    </li>
  )
}
