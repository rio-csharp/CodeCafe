import type { QuoteContent } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

export function QuoteBlock({ content }: { content: QuoteContent }) {
  return (
    <blockquote className="border-l-2 border-accent pl-4 text-muted">
      <SpanRenderer spans={content.spans} />
    </blockquote>
  )
}
