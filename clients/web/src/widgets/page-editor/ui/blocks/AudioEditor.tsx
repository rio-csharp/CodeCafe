import { useTranslation } from 'react-i18next'
import type { AudioContent } from '@/entities/block'
import { AudioBlock } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'

export interface AudioEditorProps {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
}

const FIELD_CLASS =
  'w-full rounded-md bg-transparent px-2 py-1 text-xs text-ink placeholder:text-muted/60 focus:outline focus:outline-2 focus:outline-accent'

/** Audio editing: the URL and MIME type, with a live player preview. */
export function AudioEditor({ block, onChange }: AudioEditorProps) {
  const { t } = useTranslation()
  const content = block.content as AudioContent

  return (
    <div className="flex flex-col gap-1 rounded-xl border border-line p-2">
      {content.url.length > 0 ? <AudioBlock content={content} /> : null}
      <input
        value={content.url}
        aria-label={t('editor.audioUrl')}
        placeholder={t('editor.audioUrl')}
        onChange={(event) => {
          onChange({ ...content, url: event.target.value })
        }}
        className={FIELD_CLASS}
      />
      <input
        value={content.mimeType}
        aria-label={t('editor.audioMime')}
        placeholder={t('editor.audioMime')}
        onChange={(event) => {
          onChange({ ...content, mimeType: event.target.value })
        }}
        className={FIELD_CLASS}
      />
    </div>
  )
}
