import { AlertTriangle, Loader2 } from 'lucide-react'

interface DeleteConfirmSectionProps {
  onDelete: () => void
  onCancel: () => void
  isDeleting: boolean
}

export default function DeleteConfirmSection({ onDelete, onCancel, isDeleting }: DeleteConfirmSectionProps) {
  return (
    <div className="flex items-center gap-2">
      <span className="text-xs text-red-600 flex items-center gap-1">
        <AlertTriangle className="h-3.5 w-3.5" />
        Sure?
      </span>
      <button
        type="button"
        onClick={onDelete}
        disabled={isDeleting}
        className="rounded-lg bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-700 transition-colors disabled:opacity-50"
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
        className="rounded-lg border border-gray-200 px-3 py-1.5 text-xs font-medium text-gray-700 hover:bg-gray-50 transition-colors"
      >
        Cancel
      </button>
    </div>
  )
}
