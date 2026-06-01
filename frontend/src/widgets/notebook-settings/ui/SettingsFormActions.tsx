import { Loader2, Trash2 } from 'lucide-react'
import DeleteConfirmSection from './DeleteConfirmSection'

interface SettingsFormActionsProps {
  isPending: boolean
  onCancel?: () => void
  showDeleteConfirm: boolean
  onShowDeleteConfirm: () => void
  onDelete: () => void
  onCancelDelete: () => void
  isDeleting: boolean
}

export default function SettingsFormActions({
  isPending,
  onCancel,
  showDeleteConfirm,
  onShowDeleteConfirm,
  onDelete,
  onCancelDelete,
  isDeleting,
}: SettingsFormActionsProps) {
  return (
    <div className="flex items-center justify-between pt-2">
      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={isPending}
          className="inline-flex items-center gap-2 rounded-lg bg-brand-brown px-6 py-2.5 text-sm font-medium text-text-inverse hover:opacity-90 transition-opacity disabled:opacity-50"
        >
          {isPending && <Loader2 className="h-4 w-4 animate-spin" />}
          Save Changes
        </button>
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="rounded-lg border border-border-default px-6 py-2.5 text-sm font-medium text-text-secondary hover:bg-surface-hover transition-colors"
          >
            Cancel
          </button>
        )}
      </div>

      {!showDeleteConfirm ? (
        <button
          type="button"
          onClick={onShowDeleteConfirm}
          className="inline-flex items-center gap-1.5 text-sm text-status-error hover:text-status-error-hover transition-colors"
        >
          <Trash2 className="h-4 w-4" />
          Delete
        </button>
      ) : (
        <DeleteConfirmSection
          onDelete={onDelete}
          onCancel={onCancelDelete}
          isDeleting={isDeleting}
        />
      )}
    </div>
  )
}
