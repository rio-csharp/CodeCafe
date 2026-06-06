import { useEffect, useRef, useState, useCallback, useMemo } from 'react'
import { createPortal } from 'react-dom'
import { useEditor, EditorContent } from '@tiptap/react'
import { Copy, Check } from 'lucide-react'
import { slugifyHeadingId } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { createTipTapExtensions } from '@/shared/lib/tiptapExtensions'
import { applyCodeBlockLineNumbers } from '@/shared/lib/codeBlockLineNumbers'
import { PROSE_CONTENT_CLASSES } from '@/shared/ui/proseContentClasses'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import '@/shared/styles/codeHighlight.css'

interface NotebookPageContentProps {
  page: NotebookItem
}

/**
 * Remove empty text nodes that ProseMirror rejects.
 * A text node with text === "" causes: RangeError: Empty text nodes are not allowed
 */
function sanitizeTipTapContent(content: Record<string, unknown>): Record<string, unknown> {
  if (!content || typeof content !== 'object') return content
  const clone = JSON.parse(JSON.stringify(content))

  function walk(node: unknown): unknown {
    if (typeof node !== 'object' || node === null) return node
    const n = node as Record<string, unknown>

    // Filter out empty text nodes
    if (n.type === 'text' && n.text === '') {
      return null
    }

    if (Array.isArray(n.content)) {
      n.content = n.content
        .map(walk)
        .filter((child): child is Record<string, unknown> => child !== null)
    }

    return n
  }

  return walk(clone) as Record<string, unknown>
}

function CopyOverlay({ pre }: { pre: HTMLElement }) {
  const [copied, setCopied] = useState(false)
  const timeoutRef = useRef<number | null>(null)

  useEffect(() => {
    return () => {
      if (timeoutRef.current) {
        window.clearTimeout(timeoutRef.current)
      }
    }
  }, [])

  const handleCopy = useCallback(() => {
    const code = pre.querySelector('code')
    if (!code) return
    navigator.clipboard.writeText(code.textContent ?? '').then(() => {
      setCopied(true)
      if (timeoutRef.current) window.clearTimeout(timeoutRef.current)
      timeoutRef.current = window.setTimeout(() => setCopied(false), 2000)
    })
  }, [pre])

  return createPortal(
    <button
      type="button"
      onClick={handleCopy}
      title="Copy"
      className="absolute top-2 right-2 p-1.5 rounded-md bg-surface/80 hover:bg-surface border border-border-default/60 text-text-secondary hover:text-text-primary transition-colors shadow-sm z-10"
    >
      {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
    </button>,
    pre,
  )
}

/**
 * Inner component that actually uses TipTap hooks.
 * Wrapped by ErrorBoundary so crashes don't blow up the whole page.
 */
function TipTapViewer({ content }: { content: Record<string, unknown> }) {
  const contentRef = useRef<HTMLDivElement>(null)
  const [hoveredPre, setHoveredPre] = useState<HTMLElement | null>(null)
  const hoveredPreRef = useRef(hoveredPre)
  useEffect(() => { hoveredPreRef.current = hoveredPre }, [hoveredPre])

  // TODO(#1-optimize): For read-only rendering we currently mount a full TipTap
  // Editor instance because we need DOM-level features (code-block line-number
  // gutters, copy buttons, link clicks). A lighter alternative is to use
  // @tiptap/html generateHTML() for static rendering and re-implement the
  // interactive bits on top of the raw HTML. This would remove the prosemirror
  // state-management overhead for the viewer path.
  const editor = useEditor({
    editable: false,
    content,
    extensions: createTipTapExtensions({ editable: false }),
  })

  const contentKey = JSON.stringify(content)

  // Sync content when the editor instance changes.
  // contentKey is used instead of content to avoid re-running on every render
  // when the parent passes a new object reference with identical data.
  useEffect(() => {
    if (editor) {
      editor.commands.setContent(content)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [editor, contentKey])

  // Inject heading IDs and code block line numbers after content renders
  useEffect(() => {
    if (!editor || !contentRef.current) return
    const container = contentRef.current

    const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6')
    headings.forEach((h, idx) => {
      const text = h.textContent ?? ''
      h.id = slugifyHeadingId(text, idx)
    })

    applyCodeBlockLineNumbers(container)
  }, [editor, content])

  // Track hovered code blocks via event delegation
  useEffect(() => {
    const container = contentRef.current
    if (!container) return

    const handleMouseOver = (e: MouseEvent) => {
      const pre = (e.target as HTMLElement).closest('pre')
      if (pre && container.contains(pre)) setHoveredPre(pre as HTMLElement)
    }
    const handleMouseOut = (e: MouseEvent) => {
      const pre = (e.target as HTMLElement).closest('pre')
      if (pre && pre === hoveredPreRef.current) {
        const related = e.relatedTarget as HTMLElement | null
        if (!related || !pre.contains(related)) {
          setHoveredPre(null)
        }
      }
    }

    container.addEventListener('mouseover', handleMouseOver)
    container.addEventListener('mouseout', handleMouseOut)

    return () => {
      container.removeEventListener('mouseover', handleMouseOver)
      container.removeEventListener('mouseout', handleMouseOut)
    }
  }, [content])

  return (
    <div ref={contentRef} className={PROSE_CONTENT_CLASSES}>
      <EditorContent editor={editor} />
      {hoveredPre && <CopyOverlay pre={hoveredPre} />}
    </div>
  )
}

/**
 * Plain-text fallback when TipTap fails to render.
 */
function PlainTextViewer({ text }: { text: string }) {
  return (
    <div className="prose prose-sm max-w-none">
      <p className="text-xs text-text-tertiary mb-2">Content could not be rendered in rich text. Showing plain text instead.</p>
      <pre className="whitespace-pre-wrap font-sans text-text-secondary text-sm leading-relaxed">{text}</pre>
    </div>
  )
}

export default function NotebookPageContent({ page }: NotebookPageContentProps) {
  const safeContent = useMemo(
    () => (page.contentJson ? sanitizeTipTapContent(page.contentJson) : null),
    [page.contentJson],
  )
  const hasPlainText = !!page.plainTextContent

  // Nothing to show at all
  if (!safeContent && !hasPlainText) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-sm text-text-tertiary">This page is empty.</p>
      </div>
    )
  }

  // TipTap content available — try rendering it, but guard with ErrorBoundary
  if (safeContent) {
    return (
      <ErrorBoundary
        fallback={
          hasPlainText ? (
            <PlainTextViewer text={page.plainTextContent!} />
          ) : (
            <div className="rounded-xl border border-status-error-border bg-status-error-bg p-6">
              <p className="text-sm font-semibold text-status-error">Unable to display content</p>
              <p className="mt-1 text-xs text-status-error">The page content could not be rendered.</p>
            </div>
          )
        }
      >
        <TipTapViewer content={safeContent} />
      </ErrorBoundary>
    )
  }

  // No rich content, but we have plain text
  return <PlainTextViewer text={page.plainTextContent!} />
}
