import { useTranslation } from 'react-i18next'
import type { QuoteContent, SpanDto } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'
import type { TextBlockEngineProps } from '../TextBlockEditor'
import { TextBlockEditor } from '../TextBlockEditor'

export interface QuoteEditorProps extends Omit<TextBlockEngineProps, 'spans' | 'onChange'> {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
}

/** Quote editing: the shared text engine inside the reader's quote styling. */
export function QuoteEditor({ block, onChange, ...engine }: QuoteEditorProps) {
  const { t } = useTranslation()
  const content = block.content as unknown as QuoteContent

  return (
    <div className="border-l-2 border-accent pl-2 text-muted">
      <TextBlockEditor
        {...engine}
        spans={content.spans}
        ariaLabel={t('editor.quote')}
        placeholder={t('editor.writePlaceholder')}
        onChange={(spans: SpanDto[]) => {
          onChange({ spans })
        }}
      />
    </div>
  )
}
