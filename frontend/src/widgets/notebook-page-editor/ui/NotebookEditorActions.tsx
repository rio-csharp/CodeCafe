import { Check, X } from 'lucide-react'

interface NotebookEditorActionsProps {
  onSave: () => void
  onCancel: () => void
  isSaving?: boolean
}

export default function NotebookEditorActions({ onSave, onCancel, isSaving }: NotebookEditorActionsProps) {
  return (
    <div className="flex items-center justify-end gap-2 px-4 py-3 border-t border-border-subtle">
      <button
        type="button"
        onClick={onCancel}
        disabled={isSaving}
        className="inline-flex items-center gap-1 rounded-lg border border-border-default px-4 py-2 text-sm font-medium text-text-secondary hover:bg-surface-hover transition-colors disabled:opacity-50"
      >
        <X className="h-4 w-4" />
        Cancel
      </button>
      <button
        type="button"
        onClick={onSave}
        disabled={isSaving}
        className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-4 py-2 text-sm font-medium text-text-inverse hover:opacity-90 transition-opacity disabled:opacity-50"
      >
        <Check className="h-4 w-4" />
        {isSaving ? 'Saving...' : 'Save'}
      </button>
    </div>
  )
}
