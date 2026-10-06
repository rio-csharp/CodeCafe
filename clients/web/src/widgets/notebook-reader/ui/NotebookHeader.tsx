import { useTranslation } from 'react-i18next'
import { formatRelativeTime } from '@/entities/notebook'
import type { NotebookDetails } from '@/entities/notebook'
import { Container } from '@/shared/ui'

export interface NotebookHeaderProps {
  notebook: NotebookDetails
  /** Mobile-only: the tree lives behind this disclosure below `md`. */
  contentsOpen: boolean
  onToggleContents: () => void
}

export function NotebookHeader({
  notebook,
  contentsOpen,
  onToggleContents,
}: NotebookHeaderProps) {
  const { t, i18n } = useTranslation()

  return (
    <div className="border-b border-line bg-card">
      <Container className="flex flex-col gap-3 py-8">
        <h1 className="font-display text-3xl leading-tight text-ink">{notebook.title}</h1>

        {notebook.description === null ? null : (
          <p className="max-w-2xl text-muted">{notebook.description}</p>
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

        <div className="flex flex-wrap items-center gap-4 text-xs text-muted">
          <span>{t('card.pageCount', { count: notebook.pageCount })}</span>
          <time dateTime={notebook.updatedAtUtc}>
            {t('card.updatedAt', {
              time: formatRelativeTime(notebook.updatedAtUtc, i18n.language),
            })}
          </time>
        </div>

        <button
          type="button"
          aria-expanded={contentsOpen}
          onClick={onToggleContents}
          className="self-start rounded-full border border-line px-4 py-1.5 text-sm text-ink transition-colors hover:border-accent hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent md:hidden"
        >
          {t('reader.contents')}
        </button>
      </Container>
    </div>
  )
}
