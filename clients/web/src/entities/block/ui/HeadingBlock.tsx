import { createElement } from 'react'
import type { HeadingContent } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

const MAX_LEVEL = 6

export function HeadingBlock({ content }: { content: HeadingContent }) {
  // The backend clamps to 1-6; anything outside still has to be a real tag.
  const level = Math.min(Math.max(Math.trunc(content.level) || 1, 1), MAX_LEVEL)
  const className = HEADING_CLASS[level - 1] ?? HEADING_CLASS[0]

  return createElement(
    `h${level}`,
    { className: `${className} font-display text-ink` },
    <SpanRenderer spans={content.spans} />,
  )
}

const HEADING_CLASS = [
  'mt-2 text-3xl',
  'mt-2 text-2xl',
  'mt-2 text-xl',
  'text-lg',
  'text-base',
  'text-sm uppercase tracking-wide text-muted',
] as const
