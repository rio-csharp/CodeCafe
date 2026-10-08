import { useTranslation } from 'react-i18next'
import { HEADING_CLASS } from '@/entities/block'
import type { HeadingContent } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'
import { TextBlockEditor } from '../TextBlockEditor'
import type { TextBlockEngineProps } from '../TextBlockEditor'

export interface HeadingEditorProps extends Omit<TextBlockEngineProps, 'spans' | 'onChange'> {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
}

export function HeadingEditor({ block, onChange, ...engine }: HeadingEditorProps) {
  const { t } = useTranslation()
  const content = block.content as HeadingContent
  const level = Math.min(Math.max(Math.trunc(content.level) || 1, 1), 6)

  return (
    <TextBlockEditor
      {...engine}
      spans={content.spans}
      ariaLabel={t('editor.heading')}
      placeholder={t('editor.heading')}
      className={`${HEADING_CLASS[level - 1] ?? HEADING_CLASS[0]} font-semibold text-ink`}
      onChange={(spans) => {
        onChange({ level, spans })
      }}
    />
  )
}
