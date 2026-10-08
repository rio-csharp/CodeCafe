import { useTranslation } from 'react-i18next'
import type { ImageContent } from '@/entities/block'
import { ImageBlock } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'

export interface ImageEditorProps {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
}

const FIELD_CLASS =
  'w-full rounded-md bg-transparent px-2 py-1 text-xs text-ink placeholder:text-muted/60 focus:outline focus:outline-2 focus:outline-accent'

/** Image editing: the URL plus accessibility fields, with a live preview. */
export function ImageEditor({ block, onChange }: ImageEditorProps) {
  const { t } = useTranslation()
  const content = block.content as ImageContent

  return (
    <div className="flex flex-col gap-1 rounded-xl border border-line p-2">
      {content.url.length > 0 ? <ImageBlock content={content} /> : null}
      <input
        value={content.url}
        aria-label={t('editor.imageUrl')}
        placeholder={t('editor.imageUrl')}
        onChange={(event) => {
          onChange({ ...content, url: event.target.value })
        }}
        className={FIELD_CLASS}
      />
      <div className="flex items-center gap-2">
        <input
          value={content.alt ?? ''}
          aria-label={t('editor.imageAlt')}
          placeholder={t('editor.imageAlt')}
          disabled={content.isDecorative}
          onChange={(event) => {
            onChange({ ...content, alt: event.target.value || null })
          }}
          className={`${FIELD_CLASS} flex-1 disabled:opacity-40`}
        />
        <label className="flex shrink-0 items-center gap-1 text-xs text-muted">
          <input
            type="checkbox"
            checked={content.isDecorative}
            onChange={() => {
              onChange({ ...content, isDecorative: !content.isDecorative })
            }}
            className="size-3.5 accent-accent"
          />
          {t('editor.imageDecorative')}
        </label>
      </div>
      <input
        value={content.caption ?? ''}
        aria-label={t('editor.imageCaption')}
        placeholder={t('editor.imageCaption')}
        onChange={(event) => {
          onChange({ ...content, caption: event.target.value || null })
        }}
        className={FIELD_CLASS}
      />
    </div>
  )
}
