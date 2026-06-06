import { Link } from 'react-router-dom'
import { Settings, Trash2, Loader2 } from 'lucide-react'
import type { Notebook } from '@/entities/notebook'
import { useTranslation } from 'react-i18next'

interface NotebookCardMenuProps {
  notebook: Notebook
  onDelete: (e: React.MouseEvent) => void
  isDeleting: boolean
  onClose: () => void
}

export default function NotebookCardMenu({ notebook, onDelete, isDeleting, onClose }: NotebookCardMenuProps) {
  const { t } = useTranslation()

  return (
    <div className="absolute right-0 mt-1 w-40 rounded-lg border border-border-subtle bg-surface shadow-lg z-10 py-1">
      <Link
        to={`/notes/${notebook.slug}/edit`}
        onClick={onClose}
        className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
      >
        <Settings className="h-3.5 w-3.5" />
        {t('notebook.settings')}
      </Link>
      <button
        type="button"
        data-testid="notebook-delete-button"
        onClick={onDelete}
        disabled={isDeleting}
        className="w-full flex items-center gap-2 px-3 py-2 text-xs text-status-error hover:bg-status-error-bg transition-colors disabled:opacity-50"
      >
        <Trash2 className="h-3.5 w-3.5" />
        {isDeleting ? (
          <span className="flex items-center gap-1">
            <Loader2 className="h-3 w-3 animate-spin" />
            {t('notebook.deleting')}
          </span>
        ) : (
          t('notebook.delete')
        )}
      </button>
    </div>
  )
}
