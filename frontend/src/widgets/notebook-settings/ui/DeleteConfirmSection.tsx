import { AlertTriangle, Loader2 } from 'lucide-react'

interface DeleteConfirmSectionProps {
  onDelete: () => void
  onCancel: () => void
  isDeleting: boolean
}

export default function DeleteConfirmSection({ onDelete, onCancel, isDeleting }: DeleteConfirmSectionProps) {
  return (
    <div className="flex items-center gap-2">
      <span className="text-xs text-status-error flex items-center gap-1">
        <AlertTriangle className="h-3.5 w-3.5" />
        Sure?
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
            Deleting...
          </span>
        ) : (
          'Yes, delete'
        )}
      </button>
      <button
        type="button"
        onClick={onCancel}
        className="rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
      >
        Cancel
      </button>
    </div>
  )
}
