import { useTranslation } from 'react-i18next'
import type { OutlineHeading } from '@/entities/block'
import { useActiveHeading } from '../lib/useActiveHeading'

const INDENT_PER_LEVEL_PX = 12

export interface PageOutlineProps {
  headings: readonly OutlineHeading[]
}

/**
 * The right panel's contents: the page's own headings, in reading order, with
 * the section currently under the sticky chrome lit up. Clicking one is plain
 * fragment navigation, so keyboard and middle-click behave the way links should.
 */
export function PageOutline({ headings }: PageOutlineProps) {
  const { t } = useTranslation()
  const activeId = useActiveHeading(headings.map((heading) => heading.id))

  return (
    <nav aria-label={t('reader.outline')} className="flex flex-col gap-2 p-4 text-sm">
      {headings.length === 0 ? (
        <p className="text-xs text-muted">{t('reader.noHeadings')}</p>
      ) : (
        <ul className="flex flex-col gap-1.5">
          {headings.map((heading) => {
            const isActive = heading.id === activeId
            return (
              <li key={heading.id}>
                <a
                  href={`#${heading.id}`}
                  aria-current={isActive ? 'location' : undefined}
                  style={{ paddingLeft: `${(heading.level - 1) * INDENT_PER_LEVEL_PX}px` }}
                  className={[
                    'block truncate rounded px-1 py-0.5 transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent',
                    isActive
                      ? 'bg-accent-soft font-medium text-accent-strong'
                      : 'text-muted hover:bg-muted-soft hover:text-accent-strong',
                  ].join(' ')}
                >
                  {heading.text}
                </a>
              </li>
            )
          })}
        </ul>
      )}
    </nav>
  )
}
