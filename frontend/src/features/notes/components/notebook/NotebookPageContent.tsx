import { useEffect, useRef } from 'react'
import { useEditor, EditorContent } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import CodeBlockLowlight from '@tiptap/extension-code-block-lowlight'
import { createLowlight, common } from 'lowlight'
import Color from '@tiptap/extension-color'
import { TextStyle } from '@tiptap/extension-text-style'
import Highlight from '@tiptap/extension-highlight'
import TaskList from '@tiptap/extension-task-list'
import TaskItem from '@tiptap/extension-task-item'
import { slugifyHeadingId } from '../../utils/extractOutline'
import type { NotebookItem } from '../../types'
import './codeHighlight.css'

const lowlight = createLowlight(common)

interface NotebookPageContentProps {
  page: NotebookItem
}

const COPY_ICON =
  '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>'

const CHECK_ICON =
  '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>'

function addCopyButtons(container: HTMLElement) {
  // Clean up old buttons before re-adding (prevents duplicates on content sync)
  container.querySelectorAll('.code-copy-btn').forEach((btn) => btn.remove())

  const codeBlocks = container.querySelectorAll('pre')
  codeBlocks.forEach((pre) => {
    const btn = document.createElement('button')
    btn.className =
      'code-copy-btn absolute top-2 right-2 p-1.5 rounded-md bg-white/80 hover:bg-white border border-gray-200/60 text-gray-500 hover:text-gray-800 transition-colors shadow-sm'
    btn.innerHTML = COPY_ICON
    btn.title = 'Copy'
    btn.type = 'button'

    pre.classList.add('relative')
    pre.appendChild(btn)
  })
}

function handleCopyButtonClick(e: MouseEvent) {
  const btn = (e.target as HTMLElement).closest('.code-copy-btn')
  if (!btn) return
  const pre = btn.closest('pre')
  if (!pre) return
  const code = pre.querySelector('code')
  if (!code) return
  navigator.clipboard.writeText(code.textContent ?? '').then(() => {
    btn.innerHTML = CHECK_ICON
    setTimeout(() => {
      btn.innerHTML = COPY_ICON
    }, 2000)
  })
}

export default function NotebookPageContent({ page }: NotebookPageContentProps) {
  const contentRef = useRef<HTMLDivElement>(null)

  const editor = useEditor({
    editable: false,
    content: page.contentJson,
    extensions: [
      StarterKit.configure({ codeBlock: false }),
      CodeBlockLowlight.configure({ lowlight, defaultLanguage: 'plaintext' }),
      Color,
      TextStyle,
      Highlight.configure({ multicolor: true }),
      TaskList,
      TaskItem.configure({ nested: true }),
    ],
  })

  // Sync content when it changes for the same page (e.g. after save)
  useEffect(() => {
    if (editor && page.contentJson) {
      editor.commands.setContent(page.contentJson)
    }
  }, [editor, page.contentJson])

  // Inject heading IDs + copy buttons (event delegation for cleanup)
  useEffect(() => {
    if (!contentRef.current) return
    const container = contentRef.current

    const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6')
    headings.forEach((h, idx) => {
      const text = h.textContent ?? ''
      h.id = slugifyHeadingId(text, idx)
    })
    addCopyButtons(container)
    container.addEventListener('click', handleCopyButtonClick)

    return () => {
      container.removeEventListener('click', handleCopyButtonClick)
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
      className="prose prose-sm max-w-none
        prose-headings:font-semibold prose-headings:text-black
        prose-p:text-gray-700
        prose-a:text-brand-brown
        prose-pre:bg-stone-50 prose-pre:text-gray-800 prose-pre:border prose-pre:border-stone-200 prose-pre:border-l-4 prose-pre:border-l-brand-brown prose-pre:rounded-r-lg prose-pre:rounded-l-none prose-pre:px-5 prose-pre:py-4 prose-pre:font-mono prose-pre:text-sm prose-pre:leading-relaxed prose-pre:overflow-x-auto
        [&_pre_code]:bg-transparent [&_pre_code]:text-inherit [&_pre_code]:p-0 [&_pre_code]:rounded-none
        prose-code:font-mono prose-code:text-sm prose-code:bg-stone-100 prose-code:text-gray-800 prose-code:px-1.5 prose-code:py-0.5 prose-code:rounded
        [&_ul[data-type='taskList']]:list-none [&_ul[data-type='taskList']]:pl-0
        [&_ul[data-type='taskList']_li]:flex [&_ul[data-type='taskList']_li]:items-start [&_ul[data-type='taskList']_li]:gap-2
        [&_ul[data-type='taskList']_li>label]:flex [&_ul[data-type='taskList']_li>label]:items-center [&_ul[data-type='taskList']_li>label]:mt-0.5
        [&_ul[data-type='taskList']_li>div]:flex-1 [&_ul[data-type='taskList']_p]:my-0"
    >
      <EditorContent editor={editor} />
    </div>
  )
}
