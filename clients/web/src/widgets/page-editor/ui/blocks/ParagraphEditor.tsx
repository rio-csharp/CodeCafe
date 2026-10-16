import { useTranslation } from 'react-i18next'
import type { ParagraphContent } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'
import { TextBlockEditor } from '../TextBlockEditor'
import type { TextBlockEngineProps } from '../TextBlockEditor'

export interface ParagraphEditorProps extends Omit<TextBlockEngineProps, 'spans' | 'onChange'> {
  block: EditorBlock
  onChange: (content: Record<string, unknown>, structural?: boolean) => void
}

export function ParagraphEditor({ block, onChange, ...engine }: ParagraphEditorProps) {
  const { t } = useTranslation()
  const content = block.content as ParagraphContent

  return (
    <TextBlockEditor
      {...engine}
      spans={content.spans}
      ariaLabel={t('editor.paragraph')}
      placeholder={t('editor.writePlaceholder')}
      className="text-ink"
      onChange={(spans, structural) => {
        onChange({ spans }, structural)
      }}
    />
  )
}
