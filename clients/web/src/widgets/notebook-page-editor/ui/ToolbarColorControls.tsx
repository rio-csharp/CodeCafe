import { Type, Highlighter } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { Editor } from '@tiptap/react'
import { useThemeStore } from '@/shared/model/themeStore'

interface ToolbarColorControlsProps {
  editor: Editor
}

export default function ToolbarColorControls({ editor }: ToolbarColorControlsProps) {
  const { t } = useTranslation()
  const resolvedTheme = useThemeStore((s) => s.resolvedTheme)
  const defaultColor = resolvedTheme === 'dark' ? '#f4f4f5' : '#111827'
  const defaultHighlight = resolvedTheme === 'dark' ? '#854d0e' : '#fef08a'
  const currentColor = (editor.getAttributes('textStyle').color as string | undefined) || defaultColor
  const currentHighlight = (editor.getAttributes('highlight').color as string | undefined) || defaultHighlight
  const hasColor = !!editor.getAttributes('textStyle').color
  const hasHighlight = editor.isActive('highlight')

  return (
    <>
      <label
        className={`p-1.5 rounded-md cursor-pointer transition-colors focus-within:ring-2 focus-within:ring-brand-brown ${
          hasColor ? 'bg-surface-active text-brand-brown' : 'text-text-secondary hover:bg-surface-hover hover:text-text-primary'
        }`}
        title={t('editor.toolbar.textColor')}
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
        className={`p-1.5 rounded-md cursor-pointer transition-colors focus-within:ring-2 focus-within:ring-brand-brown ${
          hasHighlight ? 'bg-surface-active text-brand-brown' : 'text-text-secondary hover:bg-surface-hover hover:text-text-primary'
        }`}
        title={t('editor.toolbar.highlight')}
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
