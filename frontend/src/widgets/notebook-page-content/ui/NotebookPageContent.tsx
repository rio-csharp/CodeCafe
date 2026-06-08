import { useLayoutEffect, useRef, useMemo } from 'react'
import { generateHTML } from '@tiptap/html'
import type { JSONContent } from '@tiptap/core'

import { slugifyHeadingId } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { createTipTapExtensions } from '@/shared/lib/tiptapExtensions'
import { applyCodeBlockLineNumbers } from '@/shared/lib/codeBlockLineNumbers'
import { highlightCodeBlocks } from '@/shared/lib/lowlight'
import { sanitizeTipTapContent } from '@/shared/lib/sanitizeTipTapContent'
import { sanitizeTipTapHtml } from '@/shared/lib/sanitizeTipTapHtml'
import { PROSE_CONTENT_CLASSES } from '@/shared/ui/proseContentClasses'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import '@/shared/styles/codeHighlight.css'

interface NotebookPageContentProps {
  page: NotebookItem
}

function createCopyButtonSvg(icon: 'copy' | 'check'): string {
  if (icon === 'check') {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>`
  }
  return `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>`
}

/**
 * Read-only TipTap renderer using generateHTML for static output.
 * No full Editor instance is mounted — only raw HTML + DOM injections
 * for heading IDs, code-block line numbers, and copy buttons.
 */
function TipTapViewer({ content }: { content: Record<string, unknown> }) {
  const containerRef = useRef<HTMLDivElement>(null)

  const extensions = useMemo(() => createTipTapExtensions({ editable: false }), [])

  const html = useMemo(() => {
    try {
      return sanitizeTipTapHtml(generateHTML(content as JSONContent, extensions))
    } catch {
      throw new Error('Failed to generate HTML from TipTap content')
    }
  }, [content, extensions])

  // Inject heading IDs, highlight code blocks, line numbers, and copy buttons
  // after HTML renders. All DOM mutations are cleaned up on the next run.
  useLayoutEffect(() => {
    const container = containerRef.current
    if (!container) return

    const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6')
    headings.forEach((h, idx) => {
      const text = h.textContent ?? ''
      h.id = slugifyHeadingId(text, idx)
    })

    highlightCodeBlocks(container)
    applyCodeBlockLineNumbers(container)

    // Inject copy buttons into every <pre><code> block
    container.querySelectorAll('pre').forEach((pre) => {
      const code = pre.querySelector('code')
      if (!code || pre.querySelector('.code-copy-btn')) return

      const btn = document.createElement('button')
      btn.type = 'button'
      btn.title = 'Copy'
      btn.className =
        'code-copy-btn absolute top-2 right-2 p-1.5 rounded-md bg-surface/80 hover:bg-surface border border-border-default/60 text-text-secondary hover:text-text-primary transition-colors shadow-sm z-10 opacity-0 pointer-events-auto'
      btn.innerHTML = createCopyButtonSvg('copy')

      const handleClick = () => {
        navigator.clipboard.writeText(code.textContent ?? '').then(() => {
          btn.innerHTML = createCopyButtonSvg('check')
          window.setTimeout(() => {
            btn.innerHTML = createCopyButtonSvg('copy')
          }, 2000)
        })
      }
      btn.addEventListener('click', handleClick)

      pre.appendChild(btn)
    })

    return () => {
      container.querySelectorAll('.code-copy-btn').forEach((btn) => btn.remove())
    }
  }, [html])

  return (
    <div ref={containerRef} className={PROSE_CONTENT_CLASSES}>
      <div dangerouslySetInnerHTML={{ __html: html }} />
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

function NotebookPageContentComponent({ page }: NotebookPageContentProps) {
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

export default function NotebookPageContent(props: NotebookPageContentProps) {
  return (
    <ErrorBoundary fallback={
      <div className="rounded-xl border border-status-error-border bg-status-error-bg p-6">
        <p className="text-sm font-semibold text-status-error">Unable to display content</p>
        <p className="mt-1 text-xs text-status-error">The page content could not be rendered.</p>
      </div>
    }>
      <NotebookPageContentComponent {...props} />
    </ErrorBoundary>
  )
}
