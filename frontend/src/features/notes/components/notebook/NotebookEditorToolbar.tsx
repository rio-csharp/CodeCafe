import type { Editor } from '@tiptap/react'
import {
  Bold,
  Italic,
  Underline as UnderlineIcon,
  Heading1,
  Heading2,
  Heading3,
  Heading4,
  List,
  ListOrdered,
  ListChecks,
  Link as LinkIcon,
  Code,
  Quote,
  Minus,
  Undo,
  Redo,
  Type,
  Highlighter,
} from 'lucide-react'

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

  const currentColor = (editor.getAttributes('textStyle').color as string | undefined) || '#000000'
  const currentHighlight = (editor.getAttributes('highlight').color as string | undefined) || '#fef08a'
  const hasColor = !!editor.getAttributes('textStyle').color
  const hasHighlight = editor.isActive('highlight')
  const currentLang = (editor.getAttributes('codeBlock').language as string | undefined) || 'plaintext'

  return (
    <div className="flex items-center gap-1 px-3 py-2 border-b border-gray-100 flex-wrap">
      {/* Headings */}
      <MenuButton
        active={editor.isActive('heading', { level: 1 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
        title="Heading 1"
      >
        <Heading1 className="h-4 w-4" />
      </MenuButton>
      <MenuButton
        active={editor.isActive('heading', { level: 2 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
        title="Heading 2"
      >
        <Heading2 className="h-4 w-4" />
      </MenuButton>
      <MenuButton
        active={editor.isActive('heading', { level: 3 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
        title="Heading 3"
      >
        <Heading3 className="h-4 w-4" />
      </MenuButton>
      <MenuButton
        active={editor.isActive('heading', { level: 4 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 4 }).run()}
        title="Heading 4"
      >
        <Heading4 className="h-4 w-4" />
      </MenuButton>
      <div className="w-px h-5 bg-gray-200 mx-1" />

      {/* Formatting */}
      <MenuButton active={editor.isActive('bold')} onClick={() => editor.chain().focus().toggleBold().run()} title="Bold">
        <Bold className="h-4 w-4" />
      </MenuButton>
      <MenuButton active={editor.isActive('italic')} onClick={() => editor.chain().focus().toggleItalic().run()} title="Italic">
        <Italic className="h-4 w-4" />
      </MenuButton>
      <MenuButton active={editor.isActive('underline')} onClick={() => editor.chain().focus().toggleUnderline().run()} title="Underline">
        <UnderlineIcon className="h-4 w-4" />
      </MenuButton>
      <div className="w-px h-5 bg-gray-200 mx-1" />

      {/* Color */}
      <label
        className={`p-1.5 rounded-md cursor-pointer transition-colors ${
          hasColor ? 'bg-stone-100 text-brand-brown' : 'text-gray-500 hover:bg-gray-50 hover:text-black'
        }`}
        title="Text color"
      >
        <Type className="h-4 w-4" style={{ color: hasColor ? currentColor : undefined }} />
        <input type="color" value={currentColor} onChange={(e) => editor.chain().focus().setColor(e.target.value).run()} className="sr-only" />
      </label>
      <label
        className={`p-1.5 rounded-md cursor-pointer transition-colors ${
          hasHighlight ? 'bg-stone-100 text-brand-brown' : 'text-gray-500 hover:bg-gray-50 hover:text-black'
        }`}
        title="Highlight"
      >
        <Highlighter className="h-4 w-4" style={{ color: hasHighlight ? currentHighlight : undefined }} />
        <input type="color" value={currentHighlight} onChange={(e) => editor.chain().focus().toggleHighlight({ color: e.target.value }).run()} className="sr-only" />
      </label>
      <div className="w-px h-5 bg-gray-200 mx-1" />

      {/* Lists */}
      <MenuButton active={editor.isActive('bulletList')} onClick={() => editor.chain().focus().toggleBulletList().run()} title="Bullet list">
        <List className="h-4 w-4" />
      </MenuButton>
      <MenuButton active={editor.isActive('orderedList')} onClick={() => editor.chain().focus().toggleOrderedList().run()} title="Numbered list">
        <ListOrdered className="h-4 w-4" />
      </MenuButton>
      <MenuButton active={editor.isActive('taskList')} onClick={() => editor.chain().focus().toggleTaskList().run()} title="Task list">
        <ListChecks className="h-4 w-4" />
      </MenuButton>
      <div className="w-px h-5 bg-gray-200 mx-1" />

      {/* Link */}
      <MenuButton active={editor.isActive('link')} onClick={handleSetLink} title="Link">
        <LinkIcon className="h-4 w-4" />
      </MenuButton>
      <div className="w-px h-5 bg-gray-200 mx-1" />

      {/* Blocks */}
      <MenuButton active={editor.isActive('codeBlock')} onClick={() => editor.chain().focus().toggleCodeBlock().run()} title="Code block">
        <Code className="h-4 w-4" />
      </MenuButton>
      {editor.isActive('codeBlock') && (
        <select
          value={currentLang}
          onChange={(e) => editor.chain().focus().setCodeBlock({ language: e.target.value }).run()}
          className="text-xs border border-gray-200 rounded px-1.5 py-0.5 bg-white text-gray-700 outline-none focus:border-gray-300 cursor-pointer"
          title="Code language"
        >
          {LANGUAGES.map((lang) => (
            <option key={lang.value} value={lang.value}>
              {lang.label}
            </option>
          ))}
        </select>
      )}
      <MenuButton active={editor.isActive('blockquote')} onClick={() => editor.chain().focus().toggleBlockquote().run()} title="Quote">
        <Quote className="h-4 w-4" />
      </MenuButton>
      <MenuButton onClick={() => editor.chain().focus().setHorizontalRule().run()} title="Horizontal rule">
        <Minus className="h-4 w-4" />
      </MenuButton>
      <div className="w-px h-5 bg-gray-200 mx-1" />

      {/* History */}
      <MenuButton onClick={() => editor.chain().focus().undo().run()} title="Undo">
        <Undo className="h-4 w-4" />
      </MenuButton>
      <MenuButton onClick={() => editor.chain().focus().redo().run()} title="Redo">
        <Redo className="h-4 w-4" />
      </MenuButton>
    </div>
  )
}
