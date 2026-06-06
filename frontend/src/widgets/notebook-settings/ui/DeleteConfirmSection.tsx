import { AlertTriangle, Loader2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'

interface DeleteConfirmSectionProps {
  onDelete: () => void
  onCancel: () => void
  isDeleting: boolean
}

export default function DeleteConfirmSection({ onDelete, onCancel, isDeleting }: DeleteConfirmSectionProps) {
  const { t } = useTranslation()

  return (
    <div className="flex items-center gap-2">
      <span className="text-xs text-status-error flex items-center gap-1">
        <AlertTriangle className="h-3.5 w-3.5" />
        {t('notebook.sure')}
      </span>
      <button
        type="button"
        onClick={onDelete}
        disabled={isDeleting}
        className="rounded-lg bg-status-error px-3 py-1.5 text-xs font-medium text-text-inverse hover:bg-status-error-hover transition-colors disabled:opacity-50"
      >
        {isDeleting ? (
          <span className="flex items-center gap-1">
            <Loader2 className="h-3 w-3 animate-spin" />
            {t('notebook.deleting')}
          </span>
        ) : (
          t('notebook.yesDelete')
        )}
      </button>
      <button
        type="button"
        onClick={onCancel}
        className="rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
      >
        {t('notebook.cancel')}
      </button>
    </div>
  )
}
