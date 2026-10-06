import type { ParagraphContent } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

export function ParagraphBlock({ content }: { content: ParagraphContent }) {
  return (
    <p className="text-ink">
      <SpanRenderer spans={content.spans} />
    </p>
  )
}
