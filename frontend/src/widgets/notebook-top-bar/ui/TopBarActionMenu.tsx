import { Link } from 'react-router-dom'
import { Link2, Settings, Trash2, Loader2 } from 'lucide-react'
import type { Notebook } from '@/entities/notebook'

interface TopBarActionMenuProps {
  notebook: Notebook
  onCopyLink: () => void
  onDelete: () => void
  isDeleting: boolean
  showDeleteConfirm: boolean
  onShowDeleteConfirm: () => void
  onCancelDelete: () => void
  onClose: () => void
}

export default function TopBarActionMenu({
  notebook,
  onCopyLink,
  onDelete,
  isDeleting,
  showDeleteConfirm,
  onShowDeleteConfirm,
  onCancelDelete,
  onClose,
}: TopBarActionMenuProps) {
  return (
    <div className="absolute right-0 mt-1.5 w-48 rounded-lg border border-border-subtle bg-surface shadow-lg z-50 py-1">
      <button
        type="button"
        onClick={onCopyLink}
        className="w-full flex items-center gap-2 px-3 py-2 text-sm text-text-secondary hover:bg-surface-hover transition-colors"
      >
        <Link2 className="h-4 w-4" />
        Copy link
      </button>
      {notebook.canEdit && (
        <>
          <Link
            to={`/notes/${notebook.slug}/edit`}
            onClick={onClose}
            className="w-full flex items-center gap-2 px-3 py-2 text-sm text-text-secondary hover:bg-surface-hover transition-colors"
          >
            <Settings className="h-4 w-4" />
            Notebook settings
          </Link>
          <div className="my-1 border-t border-border-subtle" />
          {!showDeleteConfirm ? (
            <button
              type="button"
              onClick={onShowDeleteConfirm}
              className="w-full flex items-center gap-2 px-3 py-2 text-sm text-status-error hover:bg-status-error-bg transition-colors"
            >
              <Trash2 className="h-4 w-4" />
              Delete notebook
            </button>
          ) : (
            <div className="px-3 py-2 space-y-2">
              <p className="text-xs text-status-error">Are you sure? This cannot be undone.</p>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={onDelete}
                  disabled={isDeleting}
                  className="rounded-md bg-status-error px-2.5 py-1 text-xs font-medium text-text-inverse hover:bg-status-error-hover transition-colors disabled:opacity-50"
                >
                  {isDeleting ? (
                    <span className="flex items-center gap-1">
                      <Loader2 className="h-3 w-3 animate-spin" />
                      Deleting...
                    </span>
                  ) : (
                    'Delete'
                  )}
                </button>
                <button
                  type="button"
                  onClick={onCancelDelete}
                  className="rounded-md border border-border-default px-2.5 py-1 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
