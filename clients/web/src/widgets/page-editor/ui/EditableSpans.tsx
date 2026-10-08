import type { ReactNode } from 'react'
import type { MarkDto, SpanDto } from '@/entities/block'
import { SPAN_COLOR_CLASS, SPAN_HIGHLIGHT_CLASS } from '@/entities/block'

const CODE_CLASS = 'rounded bg-muted-soft px-1 py-0.5 font-mono text-[0.875em] text-ink'
const KBD_CLASS = 'rounded border border-line bg-muted-soft px-1 font-mono text-[0.8125em]'

/**
 * The editing-time counterpart of the reader's MarkedSpan. Differences:
 * colour/highlight/name marks carry data-* attributes so parseEditableDom can
 * read them back, and nothing sets contentEditable={false} — inline code and
 * kbd text must stay editable here.
 */
function EditableMark({ mark, children }: { mark: MarkDto; children: ReactNode }) {
  switch (mark.kind) {
    case 'bold':
      return <strong>{children}</strong>
    case 'italic':
      return <em>{children}</em>
    case 'underline':
      return <u>{children}</u>
    case 'strike':
      return <s>{children}</s>
    case 'code':
      return (
        <code data-spellcheck="false" className={CODE_CLASS}>
          {children}
        </code>
      )
    case 'kbd':
      return <kbd className={KBD_CLASS}>{children}</kbd>
    case 'sup':
      return <sup>{children}</sup>
    case 'sub':
      return <sub>{children}</sub>
    case 'link':
      return (
        <a href={mark.href} className="text-accent underline decoration-accent/40 underline-offset-2">
          {children}
        </a>
      )
    case 'color':
      return (
        <span data-color={mark.name} className={SPAN_COLOR_CLASS[mark.name]}>
          {children}
        </span>
      )
    case 'highlight':
      return (
        <span data-highlight={mark.name} className={`rounded px-0.5 ${SPAN_HIGHLIGHT_CLASS[mark.name]}`}>
          {children}
        </span>
      )
    case 'abbr':
      return (
        <abbr title={mark.title} className="underline decoration-dotted underline-offset-2">
          {children}
        </abbr>
      )
    default:
      return <>{children}</>
  }
}

export function EditableSpans({ spans }: { spans: readonly SpanDto[] }) {
  return (
    // pre-wrap: Shift+Enter line breaks (\n in span text) must stay visible.
    <span className="whitespace-pre-wrap">
      {spans.map((span, index) => {
        let node: ReactNode = span.text
        for (let m = span.marks.length - 1; m >= 0; m -= 1) {
          node = (
            <EditableMark key={`m${m}`} mark={span.marks[m]!}>
              {node}
            </EditableMark>
          )
        }
        return (
          <span key={index} style={{ display: 'contents' }}>
            {node}
          </span>
        )
      })}
    </span>
  )
}
