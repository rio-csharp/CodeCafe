import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { NotebookSummary } from '../model/types'
import { formatRelativeTime } from '../lib/formatRelativeTime'

export interface NotebookCardProps {
  notebook: NotebookSummary
  /** Ownership cues (visibility badge, favorite star) — only meaningful on "mine". */
  showOwnership?: boolean
}

/**
 * A notebook as a paper card: serif monogram tile, display-serif title, then
 * the meta along the bottom edge. The whole card is the link; hover lifts it
 * toward the reader.
 */
export function NotebookCard({ notebook, showOwnership = false }: NotebookCardProps) {
  const { t, i18n } = useTranslation()

  return (
    <li className="h-full">
      <Link
        to={`/notebooks/${encodeURIComponent(notebook.slug)}`}
        className="group flex h-full flex-col rounded-xl border border-line bg-card p-5 transition-all duration-200 hover:-translate-y-0.5 hover:border-accent hover:shadow-lg hover:shadow-accent/5 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        <div className="flex items-start justify-between gap-2">
          {/* A serif monogram: the notebook's initial, stamped on warm paper. */}
          <span
            aria-hidden="true"
            className="grid size-10 shrink-0 place-items-center rounded-lg bg-accent-soft font-display text-lg text-accent-strong"
          >
            {notebook.title.trim().charAt(0).toUpperCase() || '·'}
          </span>

          <span className="flex items-center gap-1.5">
            {showOwnership && notebook.visibility !== 'Private' ? (
              <span className="rounded-full border border-line px-2 py-px text-[11px] leading-4 text-muted">
                {t(`visibility.${notebook.visibility}`)}
              </span>
            ) : null}
            {showOwnership && notebook.isFavorite ? (
              <svg
                viewBox="0 0 16 16"
                className="size-3.5 fill-accent text-accent"
                aria-label={t('home.filterFavorites')}
                role="img"
              >
                <path d="M8 1.6 9.9 5.5l4.3.6-3.1 3 .7 4.3L8 11.5l-3.8 2 .7-4.3-3.1-3 4.3-.6Z" />
              </svg>
            ) : null}
          </span>
        </div>

        <h3 className="mt-3 font-display text-lg leading-snug text-ink">{notebook.title}</h3>

        {/* Fixed two-line height keeps the grid's bottom edges aligned. */}
        <p className="mt-1 line-clamp-2 min-h-10 text-sm leading-5 text-muted">
          {notebook.description}
        </p>

        <div className="mt-4 flex items-center justify-between gap-3 border-t border-line/60 pt-3 text-xs text-muted">
          <span className="flex min-w-0 gap-2 truncate">
            {notebook.tags.slice(0, 3).map((tag) => (
              <span key={tag} className="shrink-0">
                #{tag}
              </span>
            ))}
          </span>
          <span className="shrink-0 whitespace-nowrap tabular-nums">
            {t('list.pageCount', { count: notebook.pageCount })}
            {' · '}
            {formatRelativeTime(notebook.updatedAtUtc, i18n.language)}
          </span>
        </div>
      </Link>
    </li>
  )
}
