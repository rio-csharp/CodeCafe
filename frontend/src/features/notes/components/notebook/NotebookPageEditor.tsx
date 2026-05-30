import { useEffect, useState, useCallback } from 'react'
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
import Placeholder from '@tiptap/extension-placeholder'
import CharacterCount from '@tiptap/extension-character-count'
import Youtube from '@tiptap/extension-youtube'
import FontFamily from '@tiptap/extension-font-family'
import type { NotebookItem } from '../../types'
import NotebookEditorToolbar from './NotebookEditorToolbar'
import NotebookEditorActions from './NotebookEditorActions'
import './codeHighlight.css'

const lowlight = createLowlight(common)

interface NotebookPageEditorProps {
  page: NotebookItem
  onSave: (contentJson: Record<string, unknown>) => void
  onCancel: () => void
  isSaving?: boolean
}

export default function NotebookPageEditor({ page, onSave, onCancel, isSaving }: NotebookPageEditorProps) {
  const [, forceUpdate] = useState({})

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
        class:
          'prose prose-sm max-w-none outline-none min-h-[200px] ' +
          'prose-headings:font-semibold prose-headings:text-black ' +
          'prose-p:text-gray-700 prose-a:text-brand-brown ' +
          'prose-pre:bg-stone-50 prose-pre:text-gray-800 prose-pre:border prose-pre:border-stone-200 prose-pre:border-l-4 prose-pre:border-l-brand-brown prose-pre:rounded-r-lg prose-pre:rounded-l-none prose-pre:px-5 prose-pre:py-4 prose-pre:font-mono prose-pre:text-sm prose-pre:leading-relaxed prose-pre:overflow-x-auto ' +
          '[&_pre_code]:bg-transparent [&_pre_code]:text-inherit [&_pre_code]:p-0 [&_pre_code]:rounded-none ' +
          'prose-code:font-mono prose-code:text-sm prose-code:bg-stone-100 prose-code:text-gray-800 prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded ' +
          '[&_ul[data-type=\'taskList\']]:list-none [&_ul[data-type=\'taskList\']]:pl-0 ' +
          '[&_ul[data-type=\'taskList\']_li]:flex [&_ul[data-type=\'taskList\']_li]:items-start [&_ul[data-type=\'taskList\']_li]:gap-2 ' +
          '[&_ul[data-type=\'taskList\']_li>label]:flex [&_ul[data-type=\'taskList\']_li>label]:items-center [&_ul[data-type=\'taskList\']_li>label]:mt-0.5 ' +
          '[&_ul[data-type=\'taskList\']_li>div]:flex-1 [&_ul[data-type=\'taskList\']_p]:my-0',
      },
    },
    onTransaction: () => {
      forceUpdate({})
    },
  })

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
    <div className="border border-gray-200 rounded-xl bg-white">
      <NotebookEditorToolbar editor={editor} />
      <div className="px-6 py-6 lg:px-10 lg:py-8">
        <EditorContent editor={editor} />
      </div>
      <div className="px-6 py-2 lg:px-10 border-t border-gray-100 flex justify-end">
        <span className="text-xs text-gray-400">
          {editor.storage.characterCount.characters()} characters · {editor.storage.characterCount.words()} words
        </span>
      </div>
      <NotebookEditorActions onSave={handleSave} onCancel={onCancel} isSaving={isSaving} />
    </div>
  )
}
