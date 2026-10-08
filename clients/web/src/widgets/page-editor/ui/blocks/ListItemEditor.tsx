import { useTranslation } from 'react-i18next'
import type { ListItemContent, SpanDto } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'
import type { TextBlockEngineProps } from '../TextBlockEditor'
import { TextBlockEditor } from '../TextBlockEditor'

export interface ListItemEditorProps extends Omit<TextBlockEngineProps, 'spans' | 'onChange'> {
  block: EditorBlock
  /** 1-based number within the run of consecutive ordered siblings; ignored for bullets. */
  listNumber?: number
  onChange: (content: Record<string, unknown>) => void
}

/** List editing: a bullet or run number next to the shared text engine. */
export function ListItemEditor({ block, listNumber, onChange, ...engine }: ListItemEditorProps) {
  const { t } = useTranslation()
  const content = block.content as ListItemContent
  const ordered = block.type === 'numbered-list'

  return (
    <div className="flex items-start gap-2">
      <span aria-hidden className="min-w-4 shrink-0 pt-0.5 text-right text-muted select-none">
        {ordered ? `${listNumber ?? 1}.` : '•'}
      </span>
      <div className="flex-1 text-ink">
        <TextBlockEditor
          {...engine}
          spans={content.spans}
          ariaLabel={t(ordered ? 'editor.numberedList' : 'editor.bulletedList')}
          placeholder={t('editor.writePlaceholder')}
          onChange={(spans: SpanDto[]) => {
            onChange({ spans })
          }}
        />
      </div>
    </div>
  )
}
