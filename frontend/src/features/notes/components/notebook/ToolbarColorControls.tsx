import { Type, Highlighter } from 'lucide-react'
import type { Editor } from '@tiptap/react'

interface ToolbarColorControlsProps {
  editor: Editor
}

export default function ToolbarColorControls({ editor }: ToolbarColorControlsProps) {
  const currentColor = (editor.getAttributes('textStyle').color as string | undefined) || '#000000'
  const currentHighlight = (editor.getAttributes('highlight').color as string | undefined) || '#fef08a'
  const hasColor = !!editor.getAttributes('textStyle').color
  const hasHighlight = editor.isActive('highlight')

  return (
    <>
      <label
        className={`p-1.5 rounded-md cursor-pointer transition-colors ${
          hasColor ? 'bg-stone-100 text-brand-brown' : 'text-gray-500 hover:bg-gray-50 hover:text-black'
        }`}
        title="Text color"
      >
        <Type className="h-4 w-4" style={{ color: hasColor ? currentColor : undefined }} />
        <input
          type="color"
          value={currentColor}
          onChange={(e) => editor.chain().focus().setColor(e.target.value).run()}
          className="sr-only"
        />
      </label>
      <label
        className={`p-1.5 rounded-md cursor-pointer transition-colors ${
          hasHighlight ? 'bg-stone-100 text-brand-brown' : 'text-gray-500 hover:bg-gray-50 hover:text-black'
        }`}
        title="Highlight"
      >
        <Highlighter className="h-4 w-4" style={{ color: hasHighlight ? currentHighlight : undefined }} />
        <input
          type="color"
          value={currentHighlight}
          onChange={(e) => editor.chain().focus().toggleHighlight({ color: e.target.value }).run()}
          className="sr-only"
        />
      </label>
    </>
  )
}
