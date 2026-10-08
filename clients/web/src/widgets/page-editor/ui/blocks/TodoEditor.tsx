import { useTranslation } from 'react-i18next'
import type { SpanDto, TodoContent } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'
import type { TextBlockEngineProps } from '../TextBlockEditor'
import { TextBlockEditor } from '../TextBlockEditor'

export interface TodoEditorProps extends Omit<TextBlockEngineProps, 'spans' | 'onChange'> {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
}

/** To-do editing: a live checkbox next to the shared text engine. */
export function TodoEditor({ block, onChange, ...engine }: TodoEditorProps) {
  const { t } = useTranslation()
  const content = block.content as unknown as TodoContent

  return (
    <div className="flex items-start gap-2">
      <input
        type="checkbox"
        checked={content.checked}
        aria-label={t('editor.todoToggle')}
        onChange={() => {
          onChange({ ...content, checked: !content.checked })
        }}
        className="mt-1.5 size-4 shrink-0 accent-accent"
      />
      <div className={`flex-1 ${content.checked ? 'text-muted line-through' : 'text-ink'}`}>
        <TextBlockEditor
          {...engine}
          spans={content.spans}
          ariaLabel={t('editor.todo')}
          placeholder={t('editor.writePlaceholder')}
          onChange={(spans: SpanDto[]) => {
            onChange({ ...content, spans })
          }}
        />
      </div>
    </div>
  )
}
