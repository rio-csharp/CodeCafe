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
import { slugifyHeadingId } from '../../utils/extractOutline'
import type { NotebookItem } from '../../types'
import { PROSE_CONTENT_CLASSES } from './proseContentClasses'
import './codeHighlight.css'

const lowlight = createLowlight(common)

interface NotebookPageContentProps {
  page: NotebookItem
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
      className="absolute top-2 right-2 p-1.5 rounded-md bg-white/80 hover:bg-white border border-gray-200/60 text-gray-500 hover:text-gray-800 transition-colors shadow-sm z-10"
    >
      {copied ? <Check className="h-3.5 w-3.5" /> : <Copy className="h-3.5 w-3.5" />}
    </button>,
    pre,
  )
}

export default function NotebookPageContent({ page }: NotebookPageContentProps) {
  const contentRef = useRef<HTMLDivElement>(null)
  const [hoveredPre, setHoveredPre] = useState<HTMLElement | null>(null)
  const hoveredPreRef = useRef(hoveredPre)
  useEffect(() => { hoveredPreRef.current = hoveredPre }, [hoveredPre])

  const editor = useEditor({
    editable: false,
    content: page.contentJson,
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

  // Sync content when it changes for the same page (e.g. after save)
  useEffect(() => {
    if (editor && page.contentJson) {
      editor.commands.setContent(page.contentJson)
    }
  }, [editor, page.contentJson])

  // Inject heading IDs and track hovered code blocks via event delegation
  useEffect(() => {
    if (!contentRef.current) return
    const container = contentRef.current

    const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6')
    headings.forEach((h, idx) => {
      const text = h.textContent ?? ''
      h.id = slugifyHeadingId(text, idx)
    })

    // Ensure all <pre> elements are positioned relatively for the overlay
    const codeBlocks = container.querySelectorAll('pre')
    codeBlocks.forEach((pre) => pre.classList.add('relative'))

    const handleMouseOver = (e: MouseEvent) => {
      const pre = (e.target as HTMLElement).closest('pre')
      if (pre && container.contains(pre)) setHoveredPre(pre as HTMLElement)
    }
    const handleMouseOut = (e: MouseEvent) => {
      const pre = (e.target as HTMLElement).closest('pre')
      if (pre && pre === hoveredPreRef.current) {
        // Only clear if we're actually leaving the pre, not entering a child
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
  }, [page.id, page.contentJson, editor])

  if (!page.contentJson) {
    return (
      <div className="flex items-center justify-center h-64">
        <p className="text-sm text-gray-400">This page is empty.</p>
      </div>
    )
  }

  return (
    <div
      ref={contentRef}
      key={page.id}
      className={PROSE_CONTENT_CLASSES}
    >
      <EditorContent editor={editor} />
      {hoveredPre && <CopyOverlay pre={hoveredPre} />}
    </div>
  )
}
