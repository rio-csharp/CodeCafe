import type { CalloutContent, PaletteColor } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

/** Border, tinted body and matching text — text-on-soft clears 4.5:1 in both themes. */
const VARIANT_CLASS: Record<PaletteColor, string> = {
  primary: 'border-accent bg-accent-soft text-accent-strong',
  success: 'border-success bg-success-soft text-success',
  danger: 'border-danger bg-danger-soft text-danger',
  warning: 'border-warning bg-warning-soft text-warning',
  info: 'border-info bg-info-soft text-info',
  muted: 'border-line bg-muted-soft text-muted',
}

export function CalloutBlock({ content }: { content: CalloutContent }) {
  return (
    <aside className={`whitespace-pre-wrap rounded-xl border px-4 py-3 ${VARIANT_CLASS[content.variant]}`}>
      <SpanRenderer spans={content.spans} />
    </aside>
  )
}
