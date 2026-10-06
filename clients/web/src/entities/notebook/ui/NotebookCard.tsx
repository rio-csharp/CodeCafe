import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { formatRelativeTime } from '../lib/formatRelativeTime'
import type { NotebookSummary } from '../model/types'

export interface NotebookCardProps {
  notebook: NotebookSummary
}

/** Whole-card link into the reader; the focus ring sits on the card itself. */
export function NotebookCard({ notebook }: NotebookCardProps) {
  const { t, i18n } = useTranslation()

  return (
    <Link
      to={`/notebooks/${encodeURIComponent(notebook.slug)}`}
      className="flex h-full flex-col gap-3 rounded-2xl border border-line bg-card p-5 transition-shadow hover:shadow-md hover:ring-1 hover:ring-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
    >
      <h3 className="font-display text-xl leading-snug text-ink">{notebook.title}</h3>

      {notebook.description === null ? null : (
        <p className="line-clamp-2 text-sm text-muted">{notebook.description}</p>
      )}

      {notebook.tags.length === 0 ? null : (
        <ul className="flex flex-wrap gap-2">
          {notebook.tags.map((tag) => (
            <li
              key={tag}
              className="rounded-full border border-line bg-canvas px-2.5 py-0.5 text-xs text-ink"
            >
              {tag}
            </li>
          ))}
        </ul>
      )}

      <footer className="mt-auto flex flex-wrap items-center justify-between gap-2 pt-2 text-xs text-muted">
        <span>{t('card.pageCount', { count: notebook.pageCount })}</span>
        <time dateTime={notebook.updatedAtUtc}>
          {t('card.updatedAt', {
            time: formatRelativeTime(notebook.updatedAtUtc, i18n.language),
          })}
        </time>
      </footer>
    </Link>
  )
}
