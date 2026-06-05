import { useEffect, useState, useCallback } from 'react'
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import CodeBlockLowlight from '@tiptap/extension-code-block-lowlight'
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
import Placeholder from '@tiptap/extension-placeholder'
import CharacterCount from '@tiptap/extension-character-count'
import Youtube from '@tiptap/extension-youtube'
import FontFamily from '@tiptap/extension-font-family'
import { Check, X } from 'lucide-react'
import type { NotebookItem } from '@/entities/notebook-item'
import { lowlight } from '@/shared/lib/lowlight'
import { applyCodeBlockLineNumbers } from '@/shared/lib/codeBlockLineNumbers'
import { PROSE_CONTENT_CLASSES } from '@/shared/ui/proseContentClasses'
import NotebookEditorToolbar from './NotebookEditorToolbar'
import '@/shared/styles/codeHighlight.css'

interface NotebookPageEditorProps {
  page: NotebookItem
  onSave: (contentJson: Record<string, unknown>) => void
  onCancel: () => void
  isSaving?: boolean
}

export default function NotebookPageEditor({ page, onSave, onCancel, isSaving }: NotebookPageEditorProps) {
  // Bump on every editor transaction so toolbar `isActive` checks (and any
  // other view-state reads) stay in sync. Replaces the previous `forceUpdate({})`.
  const [tick, setTick] = useState(0)
  const bumpTick = useCallback(() => setTick((t) => t + 1), [])

  const editor = useEditor({
    extensions: [
      StarterKit.configure({ codeBlock: false }),
      CodeBlockLowlight.configure({ lowlight, defaultLanguage: 'plaintext' }),
      Underline,
      Link.configure({ openOnClick: false }),
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
      Placeholder.configure({ placeholder: 'Start writing something …' }),
      CharacterCount,
    ],
    content: page.contentJson ?? { type: 'doc', content: [] },
    autofocus: 'end',
    editorProps: {
      attributes: {
        class: `${PROSE_CONTENT_CLASSES} outline-none min-h-[200px]`,
      },
    },
  })

  // Keep React in sync with editor transactions (selection moves, typing, etc.)
  useEffect(() => {
    if (!editor) return
    editor.on('transaction', bumpTick)
    return () => {
      editor.off('transaction', bumpTick)
    }
  }, [editor, bumpTick])

  // Sync code block line numbers. The function is idempotent — it short-circuits
  // when the line count hasn't changed — so running on every tick is cheap.
  useEffect(() => {
    if (!editor) return
    applyCodeBlockLineNumbers(editor.view.dom as HTMLElement)
  }, [editor, tick])

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
            className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors disabled:opacity-50"
          >
            <X className="h-3.5 w-3.5" />
            Cancel
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={isSaving}
            className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-3 py-1.5 text-xs font-medium text-text-inverse hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            <Check className="h-3.5 w-3.5" />
            {isSaving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </div>
      <div className="px-6 py-4 lg:px-10 lg:py-6">
        <EditorContent editor={editor} />
      </div>
      <div className="px-6 py-2 lg:px-10 border-t border-border-subtle flex justify-end">
        <span className="text-xs text-text-tertiary">
          {editor.storage.characterCount.characters()} characters · {editor.storage.characterCount.words()} words
        </span>
      </div>
    </div>
  )
}
