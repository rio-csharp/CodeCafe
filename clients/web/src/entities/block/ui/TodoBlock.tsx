import type { TodoContent } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

/** Read-only reflection of the writer's checkbox; M4 has no mutations. */
export function TodoBlock({ content }: { content: TodoContent }) {
  return (
    <div className="flex items-start gap-2">
      <input
        type="checkbox"
        checked={content.checked}
        disabled
        readOnly
        className="mt-1 size-4 shrink-0 accent-accent"
      />
      <span className={`whitespace-pre-wrap ${content.checked ? "text-muted line-through" : "text-ink"}`}>
        <SpanRenderer spans={content.spans} />
      </span>
    </div>
  )
}
