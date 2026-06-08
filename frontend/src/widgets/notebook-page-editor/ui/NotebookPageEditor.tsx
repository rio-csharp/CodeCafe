import { useEffect, useMemo, useRef, useState, useCallback } from 'react'
import { useEditor, EditorContent } from '@tiptap/react'
import type { Editor } from '@tiptap/react'
import { Check, X } from 'lucide-react'
import type { NotebookItem } from '@/entities/notebook-item'
import { createTipTapExtensions } from '@/shared/lib/tiptapExtensions'
import { createEmptyTipTapDocument, sanitizeTipTapContent } from '@/shared/lib/sanitizeTipTapContent'
import { applyCodeBlockLineNumbers } from '@/shared/lib/codeBlockLineNumbers'
import { PROSE_CONTENT_CLASSES } from '@/shared/ui/proseContentClasses'
import { CodeBlockCopyButton } from '@/shared/ui/CodeBlockCopyButton'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'
import NotebookEditorToolbar from './NotebookEditorToolbar'
import '@/shared/styles/codeHighlight.css'

interface NotebookPageEditorProps {
  page: NotebookItem
  onSave: (contentJson: Record<string, unknown>) => void
  onCancel: () => void
  isSaving?: boolean
}

function getMountedEditorElement(editor: Editor): HTMLElement | null {
  if (editor.isDestroyed) return null

  try {
    return editor.view.dom as HTMLElement
  } catch {
    return null
  }
}

function NotebookPageEditorComponent({ page, onSave, onCancel, isSaving }: NotebookPageEditorProps) {
  // Bump on every editor update so toolbar `isActive` checks (and any
  // other view-state reads) stay in sync. Using `update` instead of `transaction`
  // avoids unnecessary re-renders on selection-only changes.
  const [tick, setTick] = useState(0)
  const safeContent = useMemo(
    () => (page.contentJson ? sanitizeTipTapContent(page.contentJson) : createEmptyTipTapDocument()),
    [page.contentJson],
  )

  const [hoveredPre, setHoveredPre] = useState<HTMLElement | null>(null)
  const hoveredPreRef = useRef(hoveredPre)
  useEffect(() => { hoveredPreRef.current = hoveredPre }, [hoveredPre])

  const editor = useEditor({
    extensions: createTipTapExtensions({ editable: true }),
    content: safeContent,
    autofocus: 'end',
    editorProps: {
      attributes: {
        class: `${PROSE_CONTENT_CLASSES} outline-none min-h-[200px]`,
      },
    },
  })

  // Sync editor content when the external page content changes (e.g. after a
  // save roundtrip or when navigating between pages). This must run after the
  // editor instance is ready and be guarded against destroying user edits:
  // user typing only mutates the editor view, never `page.contentJson`.
  useEffect(() => {
    if (!editor || editor.isDestroyed) return
    if (editor.isEmpty && !safeContent) return
    const current = editor.getJSON()
    if (JSON.stringify(current) !== JSON.stringify(safeContent)) {
      editor.commands.setContent(safeContent, { emitUpdate: false })
    }
  }, [editor, safeContent])

  // Keep React in sync with editor updates (actual document changes only)
  useEffect(() => {
    if (!editor) return
    const bumpTick = () => setTick((t) => t + 1)
    editor.on('update', bumpTick)
    return () => {
      editor.off('update', bumpTick)
    }
  }, [editor])

  // Sync code block line numbers. The function is idempotent — it short-circuits
  // when the line count hasn't changed — so running on every tick is cheap.
  useEffect(() => {
    if (!editor) return
    let frameId: number | null = null
    let attemptsRemaining = 10

    const syncLineNumbers = () => {
      const editorElement = getMountedEditorElement(editor)
      if (editorElement) {
        applyCodeBlockLineNumbers(editorElement)
        return
      }

      if (!editor.isDestroyed && attemptsRemaining > 0) {
        attemptsRemaining -= 1
        frameId = window.requestAnimationFrame(syncLineNumbers)
      }
    }

    syncLineNumbers()

    return () => {
      if (frameId !== null) {
        window.cancelAnimationFrame(frameId)
      }
    }
  }, [editor, tick])

  // Track hovered code blocks for copy-button visibility
  useEffect(() => {
    const editorElement = getMountedEditorElement(editor)
    if (!editorElement) return

    const handleMouseOver = (e: MouseEvent) => {
      const pre = (e.target as HTMLElement).closest('pre')
      if (pre && editorElement.contains(pre)) setHoveredPre(pre as HTMLElement)
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

    editorElement.addEventListener('mouseover', handleMouseOver)
    editorElement.addEventListener('mouseout', handleMouseOut)

    return () => {
      editorElement.removeEventListener('mouseover', handleMouseOver)
      editorElement.removeEventListener('mouseout', handleMouseOut)
    }
  }, [editor])

  const handleSave = useCallback(() => {
    if (!editor) return
    const json = editor.getJSON() as Record<string, unknown>
    onSave(json)
  }, [editor, onSave])

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault()
        if (!isSaving) handleSave()
      }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [editor, isSaving, handleSave])

  if (!editor) return null

  return (
    <div className="border border-border-default rounded-xl bg-surface">
      <div className="sticky top-0 z-20 flex items-center justify-between bg-surface rounded-t-xl border-b border-border-subtle">
        <div className="flex-1 overflow-x-auto">
          <NotebookEditorToolbar editor={editor} />
        </div>
        <div className="flex items-center gap-2 px-3 py-2 shrink-0 border-l border-border-subtle">
          <button
            type="button"
            onClick={onCancel}
            disabled={isSaving}
            aria-label="Cancel editing"
            className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors disabled:opacity-50"
          >
            <X className="h-3.5 w-3.5" />
            Cancel
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving}
            aria-label="Save page"
            className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-3 py-1.5 text-xs font-medium text-text-inverse hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            <Check className="h-3.5 w-3.5" />
            {isSaving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </div>
      <div className="px-6 py-4 lg:px-10 lg:py-6">
        <EditorContent editor={editor} />
        {hoveredPre && <CodeBlockCopyButton pre={hoveredPre} />}
      </div>
      <div className="px-6 py-2 lg:px-10 border-t border-border-subtle flex justify-end">
        <span className="text-xs text-text-tertiary">
          {editor.storage.characterCount.characters()} characters · {editor.storage.characterCount.words()} words
        </span>
      </div>
    </div>
  )
}

export default function NotebookPageEditor(props: NotebookPageEditorProps) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Editor Error" description="The page editor failed to load. Your changes are safe — try refreshing." />}>
      <NotebookPageEditorComponent {...props} />
    </ErrorBoundary>
  )
}
