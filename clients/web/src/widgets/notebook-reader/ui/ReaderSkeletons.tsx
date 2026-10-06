import { useTranslation } from 'react-i18next'

const TREE_ROWS = ['a', 'b', 'c', 'd', 'e'] as const
const CONTENT_ROWS = ['a', 'b', 'c', 'd', 'e', 'f'] as const

/** Rows where the page tree will be. */
export function TreeSkeleton() {
  const { t } = useTranslation()

  return (
    <div aria-busy="true" aria-live="polite" className="flex flex-col gap-2">
      <span className="sr-only">{t('catalog.loading')}</span>
      {TREE_ROWS.map((key, index) => (
        <div
          key={key}
          className="h-6 animate-pulse rounded bg-line"
          style={{ width: `${100 - index * 12}%` }}
        />
      ))}
    </div>
  )
}

/** Paragraphs where the blocks will be. */
export function ContentSkeleton() {
  const { t } = useTranslation()

  return (
    <div aria-busy="true" aria-live="polite" className="flex flex-col gap-3">
      <span className="sr-only">{t('catalog.loading')}</span>
      <div className="h-7 w-2/3 animate-pulse rounded bg-line" />
      {CONTENT_ROWS.map((key, index) => (
        <div
          key={key}
          className="h-4 animate-pulse rounded bg-line"
          style={{ width: index % 3 === 2 ? '70%' : '100%' }}
        />
      ))}
    </div>
  )
}
