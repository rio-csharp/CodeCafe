import type { ReactNode } from 'react'
import type { MarkDto, PaletteColor, SpanDto } from '../model/types'

/** Text colour per palette entry. */
const COLOR_CLASS: Record<PaletteColor, string> = {
  primary: 'text-accent-strong',
  success: 'text-success',
  danger: 'text-danger',
  warning: 'text-warning',
  info: 'text-info',
  muted: 'text-muted',
}

/** Highlight background per palette entry; the ink colour stays inherited. */
const HIGHLIGHT_CLASS: Record<PaletteColor, string> = {
  primary: 'bg-accent-soft',
  success: 'bg-success-soft',
  danger: 'bg-danger-soft',
  warning: 'bg-warning-soft',
  info: 'bg-info-soft',
  muted: 'bg-muted-soft',
}

const CODE_CLASS =
  'rounded-sm bg-muted-soft px-1 py-0.5 font-mono text-[0.9em] text-ink'

const KBD_CLASS =
  'rounded border border-line bg-canvas px-1.5 py-0.5 font-mono text-[0.85em] text-ink shadow-sm'

export interface SpanRendererProps {
  spans: readonly SpanDto[]
}

/** Rich text as one flat run: each span carries the marks applying to all of it. */
export function SpanRenderer({ spans }: SpanRendererProps) {
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

  // Applied back to front so the first mark ends up outermost — the same order
  // `Spans` canonicalises into, which puts a link around everything it styles.
  for (let index = span.marks.length - 1; index >= 0; index -= 1) {
    const mark = span.marks[index]
    if (mark === undefined) {
      continue
    }
    content = applyMark(content, mark, `m${index}`)
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
        <code key={key} className={CODE_CLASS}>
          {content}
        </code>
      )
    case 'kbd':
      return (
        <kbd key={key} className={KBD_CLASS}>
          {content}
        </kbd>
      )
    case 'sup':
      return <sup key={key}>{content}</sup>
    case 'sub':
      return <sub key={key}>{content}</sub>
    case 'link':
      return (
        <a
          key={key}
          href={mark.href}
          target="_blank"
          rel="noopener noreferrer"
          className="text-accent-strong underline underline-offset-2 hover:text-accent"
        >
          {content}
        </a>
      )
    case 'color':
      return (
        <span key={key} className={COLOR_CLASS[mark.name]}>
          {content}
        </span>
      )
    case 'highlight':
      return (
        <span key={key} className={`${HIGHLIGHT_CLASS[mark.name]} rounded px-0.5`}>
          {content}
        </span>
      )
    case 'abbr':
      return (
        <abbr
          key={key}
          title={mark.title}
          className="cursor-help decoration-line decoration-dotted underline underline-offset-2"
        >
          {content}
        </abbr>
      )
    default:
      // An unknown mark kind is future content, not corruption — skip it.
      return content
  }
}
