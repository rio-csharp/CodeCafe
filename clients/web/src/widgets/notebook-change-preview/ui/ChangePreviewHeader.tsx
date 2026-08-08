import { Check, Pencil, Trash2, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'

interface ChangePreviewHeaderProps {
  titleId: string
  title: string
  summary?: string | null
  isDelete: boolean
  isSaving: boolean
  disableEdit: boolean
  saveEnabled: boolean
  onCancel: () => void
  onDiscard?: () => void
  onEdit: () => void
  onSave: () => void
}

export function ChangePreviewHeader({
  titleId,
  title,
  summary,
  isDelete,
  isSaving,
  disableEdit,
  saveEnabled,
  onCancel,
  onDiscard,
  onEdit,
  onSave,
}: ChangePreviewHeaderProps) {
  const { t } = useTranslation()

  return (
    <header className="border-b border-border-subtle bg-surface px-4 py-3 sm:px-6 lg:px-8">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="min-w-0">
          <p className="text-xs font-medium uppercase tracking-wide text-text-tertiary">
            {t('notebook.changePreview.label')}
          </p>
          <h2 id={titleId} className="truncate text-lg font-semibold text-text-primary">{title}</h2>
          {summary && <p className="mt-0.5 text-xs text-text-secondary">{summary}</p>}
        </div>
        <div className="flex shrink-0 items-center gap-2">
          {!isDelete && (
            <button
              type="button"
              onClick={onEdit}
              disabled={isSaving || disableEdit}
              className="inline-flex items-center gap-1.5 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Pencil className="h-3.5 w-3.5" />
              {t('notebook.changePreview.edit')}
            </button>
          )}
          {onDiscard && !isDelete && (
            <button
              type="button"
              onClick={onDiscard}
              disabled={isSaving}
              className="inline-flex items-center gap-1.5 rounded-lg border border-status-error-border px-3 py-1.5 text-xs font-medium text-status-error transition-colors hover:bg-status-error-bg disabled:cursor-not-allowed disabled:opacity-50"
            >
              <Trash2 className="h-3.5 w-3.5" />
              {t('notebook.changePreview.discard')}
            </button>
          )}
          <button
            type="button"
            onClick={onCancel}
            disabled={isSaving}
            className="inline-flex items-center gap-1.5 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary transition-colors hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
          >
            <X className="h-3.5 w-3.5" />
            {t('notebook.cancel')}
          </button>
          <button
            type="button"
            onClick={onSave}
            disabled={isSaving || !saveEnabled}
            className="inline-flex items-center gap-1.5 rounded-lg bg-brand-brown-dark dark:bg-brand-brown px-3 py-1.5 text-xs font-medium text-text-inverse transition-opacity hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Check className="h-3.5 w-3.5" />
            {isSaving
              ? t('notebook.saving')
              : isDelete
                ? t('notebook.changePreview.delete')
                : t('notebook.changePreview.save')}
          </button>
        </div>
      </div>
    </header>
  )
}
