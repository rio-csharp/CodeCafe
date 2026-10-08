import type { ListItemContent } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

export interface ListItemBlockProps {
  content: ListItemContent
  ordered: boolean
  /** 1-based number in the run of consecutive ordered items; ignored for bullets. */
  number?: number
}

export function ListItemBlock({ content, ordered, number }: ListItemBlockProps) {
  return (
    <div className="flex items-start gap-2">
      <span aria-hidden className="min-w-4 shrink-0 select-none text-right text-muted">
        {ordered ? `${number ?? 1}.` : '•'}
      </span>
      <span className="whitespace-pre-wrap text-ink">
        <SpanRenderer spans={content.spans} />
      </span>
    </div>
  )
}
