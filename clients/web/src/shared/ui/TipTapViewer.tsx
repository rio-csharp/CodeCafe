import { useMemo, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { generateHTML } from '@tiptap/html'
import type { JSONContent } from '@tiptap/core'

import { slugifyHeadingId } from '@/shared/lib/slugifyHeadingId'
import { createTipTapExtensions } from '@/shared/lib/tiptapExtensions'
import { highlightCodeBlocks } from '@/shared/lib/lowlight'
import { sanitizeTipTapContent } from '@/shared/lib/sanitizeTipTapContent'
import { sanitizeTipTapHtml } from '@/shared/lib/sanitizeTipTapHtml'
import {
  CODE_CHECK_ICON,
  CODE_COPY_FEEDBACK_MS,
  CODE_COPY_ICON,
  copyCodeFromPre,
} from '@/shared/ui/CodeBlockCopyButton'
import { PROSE_CONTENT_CLASSES } from '@/shared/ui/proseContentClasses'
import '@/shared/styles/codeHighlight.css'

interface TipTapViewerProps {
  content: Record<string, unknown>
  className?: string
}

// Extensions are stateless for generateHTML — share one instance across all
// viewers instead of building ~40 extensions per mounted viewer.
const viewerExtensions = createTipTapExtensions({ editable: false })

function createCopyButton(label: string): HTMLButtonElement {
  const btn = document.createElement('button')
  btn.type = 'button'
  btn.title = label
  btn.setAttribute('aria-label', label)
  btn.className =
    'code-copy-btn absolute top-2 right-2 p-1.5 rounded-md bg-surface/80 hover:bg-surface border border-border-default/60 text-text-secondary hover:text-text-primary transition-colors shadow-sm z-10 opacity-0 pointer-events-auto'
  btn.innerHTML = CODE_COPY_ICON
  return btn
}

export default function TipTapViewer({ content, className }: TipTapViewerProps) {
  const { t } = useTranslation()

  const html = useMemo(() => {
    const safeContent = sanitizeTipTapContent(content)
    let raw: string
    try {
      raw = sanitizeTipTapHtml(generateHTML(safeContent as JSONContent, viewerExtensions))
    } catch {
      return ''
    }

    const copyLabel = t('common.copy')
    const temp = document.createElement('div')
    temp.innerHTML = raw

    // Keep in sync with extractOutline (entities/notebook): only headings
    // with non-empty text receive an id, and the index counts only those
    // headings — otherwise outline anchors shift off-by-one after an empty
    // heading.
    let headingIndex = 0
    temp.querySelectorAll('h1, h2, h3, h4, h5, h6').forEach((h) => {
      const text = h.textContent ?? ''
      if (!text) return
      h.id = slugifyHeadingId(text, headingIndex)
      headingIndex += 1
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
        pre.appendChild(createCopyButton(copyLabel))
      }
    })

    return temp.innerHTML
  }, [content, t])

  const handleClick = useCallback((e: React.MouseEvent) => {
    const btn = (e.target as HTMLElement).closest('.code-copy-btn')
    if (!btn) return
    const pre = btn.closest('pre')
    if (!pre) return
    copyCodeFromPre(pre).then((didCopy) => {
      if (!didCopy) return
      btn.innerHTML = CODE_CHECK_ICON
      window.setTimeout(() => { btn.innerHTML = CODE_COPY_ICON }, CODE_COPY_FEEDBACK_MS)
    }).catch(() => {})
  }, [])

  return (
    <div className={className ?? PROSE_CONTENT_CLASSES} onClick={handleClick}>
      <div dangerouslySetInnerHTML={{ __html: html }} />
    </div>
  )
}
