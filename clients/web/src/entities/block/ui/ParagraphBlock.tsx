import type { ParagraphContent } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

export function ParagraphBlock({ content }: { content: ParagraphContent }) {
  return (
    <p className="whitespace-pre-wrap text-ink">
      <SpanRenderer spans={content.spans} />
    </p>
  )
}
