import { useMemo } from 'react'
import { marked } from 'marked'
import DOMPurify, { type Config as DOMPurifyConfig } from 'dompurify'

interface MarkdownRendererProps {
  className?: string
  text: string
}

const PURIFY_CONFIG: DOMPurifyConfig = {
  ALLOWED_TAGS: [
    'p',
    'br',
    'strong',
    'em',
    'code',
    'pre',
    'a',
    'ul',
    'ol',
    'li',
    'blockquote',
    'h1',
    'h2',
    'h3',
    'h4',
    'h5',
    'h6',
  ],
  ALLOWED_ATTR: ['href', 'target', 'rel'],
}

export function MarkdownRenderer({ className, text }: MarkdownRendererProps) {
  const html = useMemo(() => {
    const raw = marked.parse(text, { async: false, breaks: true, gfm: true })
    return DOMPurify.sanitize(raw, PURIFY_CONFIG)
  }, [text])

  return (
    <div
      className={[
        'prose prose-sm max-w-none prose-pre:bg-transparent prose-pre:p-0',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
      dangerouslySetInnerHTML={{ __html: html }}
    />
  )
}
