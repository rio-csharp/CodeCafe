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
 * One notebook as a shelf row. The icon tile anchors the eye, the title is the
 * link, and the chevron only slides in on hover — quiet until you reach for it.
 */
export function NotebookRow({ notebook, showOwnership = false }: NotebookRowProps) {
  const { t, i18n } = useTranslation()

  return (
    <li>
      <Link
        to={`/notebooks/${encodeURIComponent(notebook.slug)}`}
        className="group -mx-3 flex items-center gap-3.5 rounded-lg px-3 py-3 transition-colors hover:bg-card focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        {/* The shelf's visual anchor: a warm tile instead of a bare line. */}
        <span className="grid size-9 shrink-0 place-items-center rounded-lg bg-accent-soft text-accent-strong transition-transform duration-200 group-hover:-rotate-3 group-hover:scale-105">
          <svg
            viewBox="0 0 16 16"
            className="size-4"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <path d="M3 2.5h7a2 2 0 0 1 2 2v9H5a2 2 0 0 1-2-2v-9Z" />
            <path d="M5.5 5.5h4M5.5 8h4" />
          </svg>
        </span>

        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <span className="truncate font-medium text-ink">{notebook.title}</span>

            {showOwnership && notebook.visibility !== 'Private' ? (
              <span className="shrink-0 rounded-full border border-line px-2 py-px text-[11px] leading-4 text-muted">
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
          <div className="hidden shrink-0 gap-2 text-xs text-muted lg:flex">
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

        <svg
          viewBox="0 0 16 16"
          className="size-4 shrink-0 -translate-x-1 text-accent-strong opacity-0 transition-all duration-200 group-hover:translate-x-0 group-hover:opacity-100"
          fill="none"
          stroke="currentColor"
          strokeWidth="1.6"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M6 3.5 10.5 8 6 12.5" />
        </svg>
      </Link>
    </li>
  )
}
