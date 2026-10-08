import { createElement } from 'react'
import type { HeadingContent } from '../model/types'
import { HEADING_CLASS } from './headingClass'
import { SpanRenderer } from './SpanRenderer'

const MAX_LEVEL = 6

export interface HeadingBlockProps {
  content: HeadingContent
  /** Fragment target of this heading's outline entry. */
  id?: string
}

export function HeadingBlock({ content, id }: HeadingBlockProps) {
  // The backend clamps to 1-6; anything outside still has to be a real tag.
  const level = Math.min(Math.max(Math.trunc(content.level) || 1, 1), MAX_LEVEL)
  const className = HEADING_CLASS[level - 1] ?? HEADING_CLASS[0]

  return createElement(
    `h${level}`,
    { id, className: `scroll-mt-6 ${className} whitespace-pre-wrap font-semibold text-ink` },
    <SpanRenderer spans={content.spans} />,
  )
}

