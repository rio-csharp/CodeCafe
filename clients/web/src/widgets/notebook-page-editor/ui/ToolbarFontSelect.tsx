import { useTranslation } from 'react-i18next'
import type { Editor } from '@tiptap/react'

const FONTS = [
  { value: '', labelKey: 'editor.toolbar.fontDefault' },
  { value: 'serif', labelKey: 'editor.toolbar.fontSerif' },
  { value: 'sans-serif', labelKey: 'editor.toolbar.fontSans' },
  { value: 'monospace', labelKey: 'editor.toolbar.fontMono' },
] as const

interface ToolbarFontSelectProps {
  editor: Editor
}

export default function ToolbarFontSelect({ editor }: ToolbarFontSelectProps) {
  const { t } = useTranslation()
  const currentFont = (editor.getAttributes('textStyle').fontFamily as string | undefined) || ''

  return (
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
      className="text-xs border border-border-default rounded px-1.5 py-0.5 bg-surface text-text-secondary outline-none focus:border-border-hover cursor-pointer"
      title={t('editor.toolbar.fontFamily')}
    >
      {FONTS.map((f) => (
        <option key={f.value} value={f.value}>
          {t(f.labelKey)}
        </option>
      ))}
    </select>
  )
}
