import { useTranslation } from 'react-i18next'
import type { OutlineHeading } from '@/entities/block'

const INDENT_PER_LEVEL_PX = 12

export interface PageOutlineProps {
  headings: readonly OutlineHeading[]
}

/**
 * The right panel's contents: the page's own headings, in reading order.
 * Clicking one is plain fragment navigation, so keyboard and middle-click
 * behave the way links should.
 */
export function PageOutline({ headings }: PageOutlineProps) {
  const { t } = useTranslation()

  return (
    <nav aria-label={t('reader.outline')} className="flex flex-col gap-2 p-4 text-sm">
      <h2 className="text-xs font-semibold tracking-wide text-muted uppercase">
        {t('reader.outline')}
      </h2>

      {headings.length === 0 ? (
        <p className="text-xs text-muted">{t('reader.noHeadings')}</p>
      ) : (
        <ul className="flex flex-col gap-1.5">
          {headings.map((heading) => (
            <li key={heading.id}>
              <a
                href={`#${heading.id}`}
                style={{ paddingLeft: `${(heading.level - 1) * INDENT_PER_LEVEL_PX}px` }}
                className="block truncate rounded px-1 py-0.5 text-muted transition-colors hover:bg-muted-soft hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
              >
                {heading.text}
              </a>
            </li>
          ))}
        </ul>
      )}
    </nav>
  )
}
