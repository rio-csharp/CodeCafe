import { useEffect, useMemo, useState, useReducer, useCallback } from 'react'
import { useEditor, EditorContent } from '@tiptap/react'
import { Check, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { NotebookItem } from '@/entities/notebook-item'
import { createTipTapExtensions } from '@/shared/lib/tiptapExtensions'
import { getTipTapText } from '@/shared/lib/getTipTapText'
import { createEmptyTipTapDocument, sanitizeTipTapContent } from '@/shared/lib/sanitizeTipTapContent'
import { PROSE_CONTENT_CLASSES } from '@/shared/ui/proseContentClasses'
import { CodeBlockCopyButton } from '@/shared/ui/CodeBlockCopyButton'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'
import { NotebookChangePreview } from '@/widgets/notebook-change-preview'
import { useConfirmDialog } from '@/shared/ui/ConfirmDialog'
import NotebookEditorToolbar from './NotebookEditorToolbar'
import { useEditorContentSync } from './useEditorContentSync'
import { useHoveredCodeBlock } from './useHoveredCodeBlock'
import '@/shared/styles/codeHighlight.css'

interface NotebookPageEditorProps {
  page: NotebookItem
  onSave: (contentJson: Record<string, unknown>) => void
  onCancel: () => void
  isSaving?: boolean
  initialContentJson?: Record<string, unknown> | null
}

function NotebookPageEditorComponent({ page, onSave, onCancel, isSaving, initialContentJson }: NotebookPageEditorProps) {
  const { t } = useTranslation()
  // Bump on every editor update so toolbar `isActive` checks (and any
  // other view-state reads) stay in sync. Using `update` instead of `transaction`
  // avoids unnecessary re-renders on selection-only changes.
  const [, forceUpdate] = useReducer((c: number) => c + 1, 0)
  const sourceContentJson = initialContentJson ?? page.contentJson
  const safeContent = useMemo(
    () => (sourceContentJson ? sanitizeTipTapContent(sourceContentJson) : createEmptyTipTapDocument()),
    [sourceContentJson],
  )

  const [pendingPreview, setPendingPreview] = useState<{
    contentJson: Record<string, unknown>
    text: string
  } | null>(null)
  // True once the user edits the document; cleared on save/discard and when
  // external content is re-synced into the editor.
  const [dirty, setDirty] = useState(false)
  const { requestConfirm, confirmDialog } = useConfirmDialog()

  const editor = useEditor({
    extensions: createTipTapExtensions({ editable: true, placeholder: t('editor.placeholder') }),
    content: safeContent,
    autofocus: 'end',
    editorProps: {
      attributes: {
        class: `${PROSE_CONTENT_CLASSES} outline-none min-h-[200px]`,
      },
    },
  })

  useEditorContentSync(editor, safeContent)
  const hoveredPre = useHoveredCodeBlock(editor)

  // Keep React in sync with editor updates (actual document changes only)
  useEffect(() => {
    if (!editor) return
    const onUpdate = () => {
      forceUpdate()
      setDirty(true)
    }
    editor.on('update', onUpdate)
    return () => {
      editor.off('update', onUpdate)
    }
  }, [editor])

  // Warn before closing/reloading the tab with unsaved edits.
  useEffect(() => {
    if (!dirty) return
    const handler = (e: BeforeUnloadEvent) => {
      e.preventDefault()
    }
    window.addEventListener('beforeunload', handler)
    return () => window.removeEventListener('beforeunload', handler)
  }, [dirty])

  const handleSave = useCallback(() => {
    if (!editor) return
    const json = editor.getJSON() as Record<string, unknown>
    setPendingPreview({
      contentJson: json,
      text: editor.getText({ blockSeparator: '\n' }),
    })
  }, [editor])

  const handleConfirmSave = useCallback(() => {
    if (!pendingPreview) return
    setDirty(false)
    onSave(pendingPreview.contentJson)
  }, [onSave, pendingPreview])

  // Cancel discards edits — confirm first when there are unsaved changes.
  const handleCancelRequest = useCallback(async () => {
    if (dirty) {
      const ok = await requestConfirm({
        title: t('editor.discardChangesTitle'),
        danger: true,
      })
      if (!ok) return
    }
    setDirty(false)
    setPendingPreview(null)
    onCancel()
  }, [dirty, onCancel, requestConfirm, t])

  const originalText = useMemo(() => getTipTapText(safeContent), [safeContent])
  const hasJsonChanges = pendingPreview
    ? JSON.stringify(pendingPreview.contentJson) !== JSON.stringify(safeContent)
    : false

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

  if (pendingPreview) {
    return (
      <>
        <NotebookChangePreview
          afterText={pendingPreview.text}
          beforeText={originalText}
          canSave={hasJsonChanges}
          isSaving={isSaving}
          onCancel={handleCancelRequest}
          onEdit={() => setPendingPreview(null)}
          onSave={handleConfirmSave}
          title={page.title}
        />
        {confirmDialog}
      </>
    )
  }

  return (
    <div className="border border-border-default rounded-xl bg-surface">
      <div className="sticky top-0 z-20 flex items-center justify-between bg-surface rounded-t-xl border-b border-border-subtle">
        <div className="flex-1 overflow-x-auto">
          <NotebookEditorToolbar editor={editor} />
        </div>
        <div className="flex items-center gap-2 px-3 py-2 shrink-0 border-l border-border-subtle">
          <button
            type="button"
            onClick={handleCancelRequest}
            disabled={isSaving}
            aria-label={t('editor.cancelEditing')}
            className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors disabled:opacity-50"
          >
            <X className="h-3.5 w-3.5" />
            {t('notebook.cancel')}
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving}
            aria-label={t('editor.savePage')}
            className="inline-flex items-center gap-1 rounded-lg bg-brand-brown-dark dark:bg-brand-brown px-3 py-1.5 text-xs font-medium text-text-inverse hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            <Check className="h-3.5 w-3.5" />
            {isSaving ? t('notebook.saving') : t('notebook.save')}
          </button>
        </div>
      </div>
      <div className="px-6 py-4 lg:px-10 lg:py-6">
        <EditorContent editor={editor} />
        {hoveredPre && <CodeBlockCopyButton pre={hoveredPre} />}
      </div>
      <div className="px-6 py-2 lg:px-10 border-t border-border-subtle flex items-center justify-between">
        <span className={`text-xs ${dirty ? 'text-status-warning font-medium' : 'text-text-tertiary'}`}>
          {dirty ? t('editor.unsavedChanges') : ''}
        </span>
        <span className="text-xs text-text-tertiary">
          {editor.storage.characterCount.characters()} {t('notebook.characters')} · {editor.storage.characterCount.words()} {t('notebook.words')}
        </span>
      </div>
      {confirmDialog}
    </div>
  )
}

export default function NotebookPageEditor(props: NotebookPageEditorProps) {
  return (
    <ErrorBoundary fallback={<ErrorFallback />}>
      <NotebookPageEditorComponent {...props} />
    </ErrorBoundary>
  )
}
