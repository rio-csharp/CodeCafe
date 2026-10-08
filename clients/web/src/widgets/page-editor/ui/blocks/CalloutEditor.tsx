import { useTranslation } from 'react-i18next'
import type { ReactNode } from 'react'
import type { CalloutContent, PaletteColor, SpanDto } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'
import type { TextBlockEngineProps } from '../TextBlockEditor'
import { TextBlockEditor } from '../TextBlockEditor'

/** Mirrors the reader's CalloutBlock tinting. */
const VARIANT_CLASS: Record<PaletteColor, string> = {
  primary: 'border-accent bg-accent-soft text-accent-strong',
  success: 'border-success bg-success-soft text-success',
  danger: 'border-danger bg-danger-soft text-danger',
  warning: 'border-warning bg-warning-soft text-warning',
  info: 'border-info bg-info-soft text-info',
  muted: 'border-line bg-muted-soft text-muted',
}

export interface CalloutEditorProps extends Omit<TextBlockEngineProps, 'spans' | 'onChange'> {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
  /** A callout is a container: its child blocks render inside the box. */
  children?: ReactNode
}

/** Callout editing: the shared text engine inside the tinted aside. */
export function CalloutEditor({ block, onChange, children, ...engine }: CalloutEditorProps) {
  const { t } = useTranslation()
  const content = block.content as unknown as CalloutContent

  return (
    <div className={`rounded-xl border px-2 py-1 ${VARIANT_CLASS[content.variant]}`}>
      <TextBlockEditor
        {...engine}
        spans={content.spans}
        ariaLabel={t('editor.callout')}
        placeholder={t('editor.writePlaceholder')}
        onChange={(spans: SpanDto[]) => {
          onChange({ ...content, spans })
        }}
      />
      {children !== null && children !== undefined ? (
        <div className="mt-1 ml-2 text-ink">{children}</div>
      ) : null}
    </div>
  )
}
