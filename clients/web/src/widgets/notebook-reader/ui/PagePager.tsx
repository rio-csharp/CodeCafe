import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import { toPageHref } from '../lib/tree'
import type { PageNeighbour } from '../lib/tree'

export interface PagePagerProps {
  slug: string
  /** The preceding page in reading order, if any. */
  prev: PageNeighbour | null
  /** The following page in reading order, if any. */
  next: PageNeighbour | null
}

/**
 * The foot-of-the-article pager: real links, so the last line of a page is
 * also the way to the next one. Phones get the same pair as floating pills;
 * this is the desktop-only rendition, and the caller decides where it shows.
 */
export function PagePager({ slug, prev, next }: PagePagerProps) {
  const { t } = useTranslation()

  if (prev === null && next === null) {
    return null
  }

  return (
    <nav aria-label={t('reader.pageNav')} className="mt-10 grid grid-cols-2 gap-3 border-t border-line pt-6">
      {prev !== null ? <PagerLink slug={slug} page={prev} direction="prev" /> : <span />}
      {next !== null ? <PagerLink slug={slug} page={next} direction="next" /> : <span />}
    </nav>
  )
}

function PagerLink({
  slug,
  page,
  direction,
}: {
  slug: string
  page: PageNeighbour
  direction: 'prev' | 'next'
}) {
  const { t } = useTranslation()
  const isNext = direction === 'next'

  const arrow = (
    <svg
      viewBox="0 0 16 16"
      className="size-3 shrink-0"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d={isNext ? 'm6.5 3.5 4 4.5-4 4.5' : 'M9.5 3.5 5.5 8l4 4.5'} />
    </svg>
  )

  return (
    <Link
      to={toPageHref(slug, page.path)}
      className={`group flex min-w-0 flex-col gap-0.5 rounded-lg border border-line px-3 py-2.5 text-ink transition-colors hover:border-accent hover:bg-muted-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent ${
        isNext ? 'text-right' : ''
      }`}
    >
      <span
        className={`flex items-center gap-1 text-[11px] font-medium text-muted ${
          isNext ? 'justify-end' : ''
        }`}
      >
        {isNext ? null : arrow}
        {isNext ? t('reader.nextPageLabel') : t('reader.prevPageLabel')}
        {isNext ? arrow : null}
      </span>
      <span className="w-full truncate text-sm font-medium group-hover:text-accent-strong">
        {page.title}
      </span>
    </Link>
  )
}
