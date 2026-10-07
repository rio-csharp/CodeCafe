import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { BlockList, assembleBlockTree } from '@/entities/block'
import type { PageDetails } from '@/entities/page'

export interface PageEditorProps {
  page: PageDetails
  saving: boolean
  /** Set when the last save attempt failed; shown inline in the editor bar. */
  error?: string | null
  onSave: (title: string) => void
  onCancel: () => void
}

/**
 * The editing shell. E1 edits the title only; the blocks render through the
 * reader's own components so the edit view already looks like the result.
 * Escape cancels, Ctrl/Cmd+Enter saves — the same reflexes as the save bar.
 */
export function PageEditor({ page, saving, error = null, onSave, onCancel }: PageEditorProps) {
  const { t } = useTranslation()
  const [title, setTitle] = useState(page.title)
  const nodes = useMemo(() => assembleBlockTree(page.blocks), [page.blocks])

  const trimmed = title.trim()
  const canSave = trimmed.length > 0 && !saving

  return (
    <div
      onKeyDown={(event) => {
        if (event.key === 'Escape') {
          onCancel()
        } else if (event.key === 'Enter' && (event.metaKey || event.ctrlKey) && canSave) {
          onSave(trimmed)
        }
      }}
    >
      <div className="sticky top-0 z-10 -mx-4 -mt-6 mb-4 flex items-center justify-end gap-2 bg-card/95 px-4 py-2 backdrop-blur-sm sm:-mx-8 sm:px-8 xl:-mx-12 xl:px-12">
        {error !== null ? (
          <span role="alert" className="mr-auto text-xs text-danger">
            {error}
          </span>
        ) : null}
        <button
          type="button"
          onClick={onCancel}
          className="rounded-lg border border-line px-3 py-1.5 text-xs font-medium text-muted transition-colors hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
        >
          {t('editor.cancel')}
        </button>
        <button
          type="button"
          disabled={!canSave}
          onClick={() => {
            onSave(trimmed)
          }}
          className="rounded-lg bg-accent px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50"
        >
          {saving ? t('editor.saving') : t('editor.save')}
        </button>
      </div>

      <input
        value={title}
        autoFocus
        aria-label={t('editor.titlePlaceholder')}
        placeholder={t('editor.titlePlaceholder')}
        onChange={(event) => {
          setTitle(event.target.value)
        }}
        className="w-full rounded-md bg-transparent px-2 py-1 text-lg font-semibold text-ink placeholder:text-muted focus:outline focus:outline-2 focus:outline-accent"
      />

      {/* Read-only until the block editors land; already laid out like the result. */}
      <div className="mt-4 text-sm leading-relaxed">
        <BlockList nodes={nodes} />
      </div>
    </div>
  )
}
