import { useEffect, useRef, useState, useCallback } from 'react'
import { createPortal } from 'react-dom'
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import CodeBlockLowlight from '@tiptap/extension-code-block-lowlight'
import { createLowlight, common } from 'lowlight'
import Color from '@tiptap/extension-color'
import { TextStyle } from '@tiptap/extension-text-style'
import Highlight from '@tiptap/extension-highlight'
import TaskList from '@tiptap/extension-task-list'
import TaskItem from '@tiptap/extension-task-item'
import { Table } from '@tiptap/extension-table'
import TableRow from '@tiptap/extension-table-row'
import TableHeader from '@tiptap/extension-table-header'
import TableCell from '@tiptap/extension-table-cell'
import Underline from '@tiptap/extension-underline'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import TextAlign from '@tiptap/extension-text-align'
import Subscript from '@tiptap/extension-subscript'
import Superscript from '@tiptap/extension-superscript'
import CharacterCount from '@tiptap/extension-character-count'
import Youtube from '@tiptap/extension-youtube'
import FontFamily from '@tiptap/extension-font-family'
import { Copy, Check } from 'lucide-react'
import { slugifyHeadingId } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { PROSE_CONTENT_CLASSES } from '@/shared/ui/proseContentClasses'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import '@/widgets/notebook-page-editor/ui/codeHighlight.css'

const lowlight = createLowlight(common)

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

  const handleCopy = useCallback(() => {
    const code = pre.querySelector('code')
    if (!code) return
    navigator.clipboard.writeText(code.textContent ?? '').then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
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

  const editor = useEditor({
    editable: false,
    content,
    extensions: [
      StarterKit.configure({ codeBlock: false }),
      CodeBlockLowlight.configure({ lowlight, defaultLanguage: 'plaintext' }),
      Underline,
      Link.configure({ openOnClick: true }),
      FontFamily,
      Color,
      TextStyle,
      Highlight.configure({ multicolor: true }),
      TextAlign.configure({ types: ['heading', 'paragraph'] }),
      Subscript,
      Superscript,
      Image,
      Youtube.configure({ nocookie: true }),
      TaskList,
      TaskItem.configure({ nested: true }),
      Table.configure({ resizable: true }),
      TableRow,
      TableHeader,
      TableCell,
      CharacterCount,
    ],
  })

  // Sync content when the editor instance changes
  useEffect(() => {
    if (editor) {
      editor.commands.setContent(content)
    }
  }, [editor, content])

  // Inject heading IDs and code block line numbers after content renders
  useEffect(() => {
    if (!editor || !contentRef.current) return
    const container = contentRef.current

    const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6')
    headings.forEach((h, idx) => {
      const text = h.textContent ?? ''
      h.id = slugifyHeadingId(text, idx)
    })

    container.querySelectorAll('pre').forEach((pre) => {
      const code = pre.querySelector('code')
      if (!code) return
      const lineCount = code.textContent?.split('\n').length || 1
      let lineNumbers = pre.querySelector('.line-numbers') as HTMLElement | null
      if (!lineNumbers) {
        lineNumbers = document.createElement('div')
        lineNumbers.className = 'line-numbers'
        lineNumbers.setAttribute('aria-hidden', 'true')
        pre.insertBefore(lineNumbers, code)
      }
      const existingSpans = lineNumbers.querySelectorAll('span')
      if (existingSpans.length === lineCount) return
      lineNumbers.innerHTML = ''
      for (let i = 1; i <= lineCount; i++) {
        const span = document.createElement('span')
        span.textContent = String(i)
        lineNumbers.appendChild(span)
      }
    })
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
  const safeContent = page.contentJson ? sanitizeTipTapContent(page.contentJson) : null
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
