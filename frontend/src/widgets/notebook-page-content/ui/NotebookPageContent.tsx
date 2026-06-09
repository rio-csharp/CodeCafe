import { useMemo, useCallback } from 'react'
import { generateHTML } from '@tiptap/html'
import type { JSONContent } from '@tiptap/core'

import { slugifyHeadingId } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { createTipTapExtensions } from '@/shared/lib/tiptapExtensions'
import { highlightCodeBlocks } from '@/shared/lib/lowlight'
import { sanitizeTipTapContent } from '@/shared/lib/sanitizeTipTapContent'
import { sanitizeTipTapHtml } from '@/shared/lib/sanitizeTipTapHtml'
import { PROSE_CONTENT_CLASSES } from '@/shared/ui/proseContentClasses'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import '@/shared/styles/codeHighlight.css'

interface NotebookPageContentProps {
  page: NotebookItem
}

const COPY_ICON = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>`

const CHECK_ICON = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>`

const COPY_BTN_HTML = `<button type="button" title="Copy" class="code-copy-btn absolute top-2 right-2 p-1.5 rounded-md bg-surface/80 hover:bg-surface border border-border-default/60 text-text-secondary hover:text-text-primary transition-colors shadow-sm z-10 opacity-0 pointer-events-auto">${COPY_ICON}</button>`

function TipTapViewer({ content }: { content: Record<string, unknown> }) {
  const extensions = useMemo(() => createTipTapExtensions({ editable: false }), [])

  const html = useMemo(() => {
    const raw = sanitizeTipTapHtml(generateHTML(content as JSONContent, extensions))
    const temp = document.createElement('div')
    temp.innerHTML = raw

    temp.querySelectorAll('h1, h2, h3, h4, h5, h6').forEach((h, idx) => {
      h.id = slugifyHeadingId(h.textContent ?? '', idx)
    })

    highlightCodeBlocks(temp)

    temp.querySelectorAll('p').forEach((p) => {
      if (!p.innerHTML.trim()) p.remove()
    })

    temp.querySelectorAll('ul[data-type="taskList"] input[type="checkbox"]').forEach((input) => {
      input.setAttribute('disabled', 'disabled')
    })

    temp.querySelectorAll('pre').forEach((pre) => {
      if (pre.querySelector('code') && !pre.querySelector('.code-copy-btn')) {
        pre.insertAdjacentHTML('beforeend', COPY_BTN_HTML)
      }
    })

    return temp.innerHTML
  }, [content, extensions])

  const handleClick = useCallback((e: React.MouseEvent) => {
    const btn = (e.target as HTMLElement).closest('.code-copy-btn')
    if (!btn) return
    const pre = btn.closest('pre')
    const code = pre?.querySelector('code')
    if (!code) return
    navigator.clipboard.writeText(code.textContent ?? '').then(() => {
      btn.innerHTML = CHECK_ICON
      window.setTimeout(() => { btn.innerHTML = COPY_ICON }, 2000)
    })
  }, [])

  return (
    <div className={PROSE_CONTENT_CLASSES} onClick={handleClick}>
      <div dangerouslySetInnerHTML={{ __html: html }} />
    </div>
  )
}

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

  if (!safeContent && !hasPlainText) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-sm text-text-tertiary">This page is empty.</p>
      </div>
    )
  }

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
