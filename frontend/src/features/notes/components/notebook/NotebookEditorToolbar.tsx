import type { Editor } from '@tiptap/react'
import {
  Bold,
  Italic,
  Underline as UnderlineIcon,
  Strikethrough,
  Subscript,
  Superscript,
  Heading1,
  Heading2,
  Heading3,
  Heading4,
  AlignLeft,
  AlignCenter,
  AlignRight,
  AlignJustify,
  List,
  ListOrdered,
  ListChecks,
  Link as LinkIcon,
  Image as ImageIcon,
  Video,
  Code,
  Quote,
  Minus,
  Undo,
  Redo,
  Type,
  Highlighter,
  Table as TableIcon,
  Trash2,
  Plus,
} from 'lucide-react'
import ToolbarGroup from './ToolbarGroup'

const MenuButton = ({
  active,
  onClick,
  children,
  title,
}: {
  active?: boolean
  onClick: () => void
  children: React.ReactNode
  title: string
}) => (
  <button
    type="button"
    onClick={onClick}
    title={title}
    className={`p-1.5 rounded-md transition-colors ${
      active ? 'bg-stone-100 text-brand-brown' : 'text-gray-500 hover:bg-gray-50 hover:text-black'
    }`}
  >
    {children}
  </button>
)

const LANGUAGES = [
  { value: 'plaintext', label: 'Plain text' },
  { value: 'javascript', label: 'JavaScript' },
  { value: 'typescript', label: 'TypeScript' },
  { value: 'python', label: 'Python' },
  { value: 'bash', label: 'Bash' },
  { value: 'json', label: 'JSON' },
  { value: 'html', label: 'HTML' },
  { value: 'css', label: 'CSS' },
  { value: 'csharp', label: 'C#' },
  { value: 'sql', label: 'SQL' },
  { value: 'markdown', label: 'Markdown' },
  { value: 'java', label: 'Java' },
  { value: 'go', label: 'Go' },
  { value: 'rust', label: 'Rust' },
  { value: 'yaml', label: 'YAML' },
  { value: 'xml', label: 'XML' },
]

const FONTS = [
  { value: '', label: 'Default' },
  { value: 'serif', label: 'Serif' },
  { value: 'sans-serif', label: 'Sans Serif' },
  { value: 'monospace', label: 'Monospace' },
]

interface NotebookEditorToolbarProps {
  editor: Editor
}

export default function NotebookEditorToolbar({ editor }: NotebookEditorToolbarProps) {
  const handleSetLink = () => {
    const previousUrl = editor.getAttributes('link').href as string | undefined
    const url = window.prompt('URL', previousUrl)
    if (url === null) return
    if (url === '') {
      editor.chain().focus().extendMarkRange('link').unsetLink().run()
    } else {
      editor.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
    }
  }

  const handleInsertImage = () => {
    const url = window.prompt('Image URL')
    if (url) {
      editor.chain().focus().setImage({ src: url }).run()
    }
  }

  const handleInsertYoutube = () => {
    const url = window.prompt('YouTube URL')
    if (url) {
      editor.chain().focus().setYoutubeVideo({ src: url }).run()
    }
  }

  const currentColor = (editor.getAttributes('textStyle').color as string | undefined) || '#000000'
  const currentHighlight = (editor.getAttributes('highlight').color as string | undefined) || '#fef08a'
  const hasColor = !!editor.getAttributes('textStyle').color
  const hasHighlight = editor.isActive('highlight')
  const currentLang = (editor.getAttributes('codeBlock').language as string | undefined) || 'plaintext'
  const currentFont = (editor.getAttributes('textStyle').fontFamily as string | undefined) || ''

  return (
    <div className="flex items-center gap-1 px-3 py-2 border-b border-gray-100 flex-wrap">
      <ToolbarGroup showDivider>
        <MenuButton active={editor.isActive('heading', { level: 1 })} onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()} title="Heading 1"><Heading1 className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('heading', { level: 2 })} onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} title="Heading 2"><Heading2 className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('heading', { level: 3 })} onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()} title="Heading 3"><Heading3 className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('heading', { level: 4 })} onClick={() => editor.chain().focus().toggleHeading({ level: 4 }).run()} title="Heading 4"><Heading4 className="h-4 w-4" /></MenuButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <MenuButton active={editor.isActive('bold')} onClick={() => editor.chain().focus().toggleBold().run()} title="Bold"><Bold className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('italic')} onClick={() => editor.chain().focus().toggleItalic().run()} title="Italic"><Italic className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('underline')} onClick={() => editor.chain().focus().toggleUnderline().run()} title="Underline"><UnderlineIcon className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('strike')} onClick={() => editor.chain().focus().toggleStrike().run()} title="Strikethrough"><Strikethrough className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('subscript')} onClick={() => editor.chain().focus().toggleSubscript().run()} title="Subscript"><Subscript className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('superscript')} onClick={() => editor.chain().focus().toggleSuperscript().run()} title="Superscript"><Superscript className="h-4 w-4" /></MenuButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <select
          value={currentFont}
          onChange={(e) => {
            const font = e.target.value
            if (font) {
              editor.chain().focus().setFontFamily(font).run()
            } else {
              editor.chain().focus().unsetFontFamily().run()
            }
          }}
          className="text-xs border border-gray-200 rounded px-1.5 py-0.5 bg-white text-gray-700 outline-none focus:border-gray-300 cursor-pointer"
          title="Font family"
        >
          {FONTS.map((f) => (
            <option key={f.value} value={f.value}>{f.label}</option>
          ))}
        </select>
        <label className={`p-1.5 rounded-md cursor-pointer transition-colors ${hasColor ? 'bg-stone-100 text-brand-brown' : 'text-gray-500 hover:bg-gray-50 hover:text-black'}`} title="Text color">
          <Type className="h-4 w-4" style={{ color: hasColor ? currentColor : undefined }} />
          <input type="color" value={currentColor} onChange={(e) => editor.chain().focus().setColor(e.target.value).run()} className="sr-only" />
        </label>
        <label className={`p-1.5 rounded-md cursor-pointer transition-colors ${hasHighlight ? 'bg-stone-100 text-brand-brown' : 'text-gray-500 hover:bg-gray-50 hover:text-black'}`} title="Highlight">
          <Highlighter className="h-4 w-4" style={{ color: hasHighlight ? currentHighlight : undefined }} />
          <input type="color" value={currentHighlight} onChange={(e) => editor.chain().focus().toggleHighlight({ color: e.target.value }).run()} className="sr-only" />
        </label>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <MenuButton active={editor.isActive({ textAlign: 'left' })} onClick={() => editor.chain().focus().setTextAlign('left').run()} title="Align left"><AlignLeft className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive({ textAlign: 'center' })} onClick={() => editor.chain().focus().setTextAlign('center').run()} title="Align center"><AlignCenter className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive({ textAlign: 'right' })} onClick={() => editor.chain().focus().setTextAlign('right').run()} title="Align right"><AlignRight className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive({ textAlign: 'justify' })} onClick={() => editor.chain().focus().setTextAlign('justify').run()} title="Align justify"><AlignJustify className="h-4 w-4" /></MenuButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <MenuButton active={editor.isActive('bulletList')} onClick={() => editor.chain().focus().toggleBulletList().run()} title="Bullet list"><List className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('orderedList')} onClick={() => editor.chain().focus().toggleOrderedList().run()} title="Numbered list"><ListOrdered className="h-4 w-4" /></MenuButton>
        <MenuButton active={editor.isActive('taskList')} onClick={() => editor.chain().focus().toggleTaskList().run()} title="Task list"><ListChecks className="h-4 w-4" /></MenuButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <MenuButton active={editor.isActive('link')} onClick={handleSetLink} title="Link"><LinkIcon className="h-4 w-4" /></MenuButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <MenuButton active={editor.isActive('codeBlock')} onClick={() => editor.chain().focus().toggleCodeBlock().run()} title="Code block"><Code className="h-4 w-4" /></MenuButton>
        {editor.isActive('codeBlock') && (
          <select value={currentLang} onChange={(e) => editor.chain().focus().setCodeBlock({ language: e.target.value }).run()} className="text-xs border border-gray-200 rounded px-1.5 py-0.5 bg-white text-gray-700 outline-none focus:border-gray-300 cursor-pointer" title="Code language">
            {LANGUAGES.map((lang) => (
              <option key={lang.value} value={lang.value}>{lang.label}</option>
            ))}
          </select>
        )}
        <MenuButton active={editor.isActive('blockquote')} onClick={() => editor.chain().focus().toggleBlockquote().run()} title="Quote"><Quote className="h-4 w-4" /></MenuButton>
        <MenuButton onClick={() => editor.chain().focus().setHorizontalRule().run()} title="Horizontal rule"><Minus className="h-4 w-4" /></MenuButton>
        <MenuButton onClick={handleInsertImage} title="Insert image"><ImageIcon className="h-4 w-4" /></MenuButton>
        <MenuButton onClick={handleInsertYoutube} title="Insert YouTube video"><Video className="h-4 w-4" /></MenuButton>
      </ToolbarGroup>
      <ToolbarGroup showDivider>
        <MenuButton onClick={() => editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()} title="Insert table"><TableIcon className="h-4 w-4" /></MenuButton>
        {editor.isActive('table') && (
          <>
            <MenuButton onClick={() => editor.chain().focus().addColumnBefore().run()} title="Add column before"><Plus className="h-4 w-4" /></MenuButton>
            <MenuButton onClick={() => editor.chain().focus().addColumnAfter().run()} title="Add column after"><Plus className="h-4 w-4" /></MenuButton>
            <MenuButton onClick={() => editor.chain().focus().deleteColumn().run()} title="Delete column"><Minus className="h-4 w-4" /></MenuButton>
            <MenuButton onClick={() => editor.chain().focus().addRowBefore().run()} title="Add row before"><Plus className="h-4 w-4" /></MenuButton>
            <MenuButton onClick={() => editor.chain().focus().addRowAfter().run()} title="Add row after"><Plus className="h-4 w-4" /></MenuButton>
            <MenuButton onClick={() => editor.chain().focus().deleteRow().run()} title="Delete row"><Minus className="h-4 w-4" /></MenuButton>
            <MenuButton onClick={() => editor.chain().focus().deleteTable().run()} title="Delete table"><Trash2 className="h-4 w-4" /></MenuButton>
          </>
        )}
      </ToolbarGroup>
      <ToolbarGroup>
        <MenuButton onClick={() => editor.chain().focus().undo().run()} title="Undo"><Undo className="h-4 w-4" /></MenuButton>
        <MenuButton onClick={() => editor.chain().focus().redo().run()} title="Redo"><Redo className="h-4 w-4" /></MenuButton>
      </ToolbarGroup>
    </div>
  )
}
