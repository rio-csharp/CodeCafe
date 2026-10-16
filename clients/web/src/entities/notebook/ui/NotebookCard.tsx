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
 * A notebook as a compact card, 3.4-style: hash-picked icon tile, dense meta,
 * and an author row — a shared notebook says whose it is. The whole card is a
 * link; hover lifts it slightly.
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
        className="flex h-full flex-col rounded-xl border border-line bg-card p-5 transition-all duration-200 hover:-translate-y-0.5 hover:border-accent hover:shadow-md focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        <div className="flex items-start gap-3.5">
          <span className="grid size-10 shrink-0 place-items-center rounded-lg bg-muted-soft text-muted">
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

          <div className="min-w-0 flex-1 pr-6">
            <div className="flex items-center gap-2">
              <h3 className="truncate text-sm font-semibold text-ink">{notebook.title}</h3>
              {showOwnership && notebook.visibility !== 'Private' ? (
                <span className="shrink-0 rounded-full border border-line px-2 py-px text-[11px] leading-4 text-muted">
                  {t(`visibility.${notebook.visibility}`)}
                </span>
              ) : null}
            </div>
            {/* Fixed two-line height keeps the grid's bottom edges aligned. */}
            <p className="mt-1 line-clamp-2 min-h-8 text-xs leading-relaxed text-muted">
              {notebook.description ?? t('card.noDescription')}
            </p>
          </div>
        </div>

        <div className="mt-3 flex items-center gap-1.5 text-[11px] text-muted">
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

        <div className="mt-3 flex items-center justify-between gap-3">
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
