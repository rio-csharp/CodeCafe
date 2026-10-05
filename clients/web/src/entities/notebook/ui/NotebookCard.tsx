import { useTranslation } from 'react-i18next'
import { formatRelativeTime } from '../lib/formatRelativeTime'
import type { NotebookSummary } from '../model/types'

export interface NotebookCardProps {
  notebook: NotebookSummary
}

/**
 * Plain `<article>` in M1 — the reader page is a later milestone, so there is
 * nothing to link to yet.
 */
export function NotebookCard({ notebook }: NotebookCardProps) {
  const { t, i18n } = useTranslation()

  return (
    <article className="flex h-full flex-col gap-3 rounded-2xl border border-latte bg-paper p-5 transition-shadow hover:shadow-md hover:ring-1 hover:ring-caramel">
      <h3 className="font-display text-xl leading-snug text-espresso">{notebook.title}</h3>

      {notebook.description === null ? null : (
        <p className="line-clamp-2 text-sm text-mocha">{notebook.description}</p>
      )}

      {notebook.tags.length === 0 ? null : (
        <ul className="flex flex-wrap gap-2">
          {notebook.tags.map((tag) => (
            <li
              key={tag}
              className="rounded-full border border-latte bg-cream px-2.5 py-0.5 text-xs text-roast"
            >
              {tag}
            </li>
          ))}
        </ul>
      )}

      <footer className="mt-auto flex flex-wrap items-center justify-between gap-2 pt-2 text-xs text-mocha">
        <span>{t('card.pageCount', { count: notebook.pageCount })}</span>
        <time dateTime={notebook.updatedAtUtc}>
          {t('card.updatedAt', {
            time: formatRelativeTime(notebook.updatedAtUtc, i18n.language),
          })}
        </time>
      </footer>
    </article>
  )
}
