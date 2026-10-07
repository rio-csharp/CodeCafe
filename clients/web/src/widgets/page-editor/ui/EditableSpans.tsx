import type { ReactNode } from 'react'
import { SPAN_COLOR_CLASS, SPAN_HIGHLIGHT_CLASS } from '@/entities/block'
import type { MarkDto, SpanDto } from '@/entities/block'

/**
 * The edit-mode span renderer. Identical styling to the reader's
 * `SpanRenderer`, but colour/highlight marks also carry `data-*` attributes:
 * `parseEditableDom` reads the DOM back into spans, and classes alone are not
 * a reliable channel.
 */
export function EditableSpans({ spans }: { spans: readonly SpanDto[] }) {
  return (
    <>
      {spans.map((span, index) => (
        <MarkedSpan key={index} span={span} />
      ))}
    </>
  )
}

function MarkedSpan({ span }: { span: SpanDto }) {
  let content: ReactNode = span.text
  for (let index = span.marks.length - 1; index >= 0; index -= 1) {
    const mark = span.marks[index]
    if (mark !== undefined) {
      content = applyMark(content, mark, `m${index}`)
    }
  }
  return <>{content}</>
}

function applyMark(content: ReactNode, mark: MarkDto, key: string): ReactNode {
  switch (mark.kind) {
    case 'bold':
      return <strong key={key}>{content}</strong>
    case 'italic':
      return <em key={key}>{content}</em>
    case 'underline':
      return <u key={key}>{content}</u>
    case 'strike':
      return <s key={key}>{content}</s>
    case 'code':
      return (
        <code key={key} className="rounded-sm bg-muted-soft px-1 py-0.5 font-mono text-[0.9em] text-ink">
          {content}
        </code>
      )
    case 'kbd':
      return (
        <kbd key={key} className="rounded border border-line bg-canvas px-1.5 py-0.5 font-mono text-[0.85em] text-ink shadow-sm">
          {content}
        </kbd>
      )
    case 'sup':
      return <sup key={key}>{content}</sup>
    case 'sub':
      return <sub key={key}>{content}</sub>
    case 'link':
      return (
        // In edit mode the link is text, not a navigation target.
        <a key={key} href={mark.href} className="text-accent-strong underline underline-offset-2">
          {content}
        </a>
      )
    case 'color':
      return (
        <span key={key} data-color={mark.name} className={SPAN_COLOR_CLASS[mark.name]}>
          {content}
        </span>
      )
    case 'highlight':
      return (
        <span key={key} data-highlight={mark.name} className={`${SPAN_HIGHLIGHT_CLASS[mark.name]} rounded px-0.5`}>
          {content}
        </span>
      )
    case 'abbr':
      return (
        <abbr key={key} title={mark.title} className="cursor-help decoration-line decoration-dotted underline underline-offset-2">
          {content}
        </abbr>
      )
    default:
      return content
  }
}
