import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { ReactNode } from 'react'
import type { NotebookSummary } from '../model/types'
import { formatRelativeTime } from '../lib/formatRelativeTime'
import { notebookIcon } from '../lib/notebookIcon'

export interface NotebookCardProps {
  notebook: NotebookSummary
  /** Ownership cues (visibility badge) — only meaningful on "mine". */
  showOwnership?: boolean
  /** Sits on the card's top-right corner — the favorite star, in practice. */
  cornerAction?: ReactNode
}

/**
 * A notebook link with its description, tags, page count and author.
 * Independent corner actions remain outside the link for valid keyboard navigation.
 */
export function NotebookCard({ notebook, showOwnership = false, cornerAction }: NotebookCardProps) {
  const { t, i18n } = useTranslation()
  // Tolerate a stale backend that predates ownerDisplayName: no attribution
  // beats a thrown page.
  const ownerName = notebook.ownerDisplayName ?? ''
  const ownerInitial = ownerName.trim().charAt(0).toUpperCase()

  return (
    <li className="group relative h-full">
      <Link
        to={`/notebooks/${encodeURIComponent(notebook.slug)}`}
        className="flex h-full flex-col rounded-2xl border border-line bg-card p-6 transition-all duration-200 hover:-translate-y-1 hover:border-accent/60 hover:shadow-lg focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        <div className="flex flex-col items-start gap-4">
          <span className="grid size-11 shrink-0 place-items-center rounded-xl bg-accent-soft text-accent-strong">
            <svg
              viewBox="0 0 16 16"
              className="size-5"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.4"
              strokeLinecap="round"
              strokeLinejoin="round"
              aria-hidden="true"
            >
              {notebookIcon(notebook.title)}
            </svg>
          </span>

          <div className="min-w-0 w-full flex-1">
            <div className="flex items-center gap-2">
              <h3 className="truncate text-base font-semibold text-ink">{notebook.title}</h3>
              {showOwnership && notebook.visibility !== 'Private' ? (
                <span className="shrink-0 rounded-full border border-line px-2 py-px text-[11px] leading-4 text-muted">
                  {t(`visibility.${notebook.visibility}`)}
                </span>
              ) : null}
            </div>
            {/* Fixed two-line height keeps the grid's bottom edges aligned. */}
            <p className="mt-2 line-clamp-2 min-h-10 text-sm leading-relaxed text-muted">
              {notebook.description ?? t('card.noDescription')}
            </p>
          </div>
        </div>

        {notebook.tags.length > 0 ? (
          <div className="mt-4 flex flex-wrap gap-1.5">
            {notebook.tags.slice(0, 3).map((tag) => (
              <span key={tag} className="max-w-full truncate rounded-md bg-muted-soft px-2 py-1 text-[11px] text-muted">#{tag}</span>
            ))}
          </div>
        ) : null}
        <div className="mt-auto flex items-center gap-1.5 pt-5 text-xs text-muted">
          <svg
            viewBox="0 0 16 16"
            className="size-3"
            fill="none"
            stroke="currentColor"
            strokeWidth="1.4"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <path d="M4 2.5h5.5L12 5v8.5a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1v-10a1 1 0 0 1 1-1Z" />
          </svg>
          {t('list.pageCount', { count: notebook.pageCount })}
        </div>

        <div className="mt-4 flex items-center justify-between gap-3 border-t border-line/70 pt-4">
          <span className="flex min-w-0 items-center gap-2">
            <span
              aria-hidden="true"
              className="grid size-5 shrink-0 place-items-center rounded-full bg-accent-soft text-[10px] font-medium text-accent-strong"
            >
              {ownerInitial === '' ? '·' : ownerInitial}
            </span>
            <span className="truncate text-xs text-muted">{ownerName}</span>
          </span>
          <span className="shrink-0 text-xs whitespace-nowrap text-muted tabular-nums">
            {formatRelativeTime(notebook.updatedAtUtc, i18n.language)}
          </span>
        </div>
      </Link>

      {cornerAction}
    </li>
  )
}
