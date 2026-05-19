import { Link } from 'react-router-dom'
import { Settings, Trash2, Loader2 } from 'lucide-react'
import type { Notebook } from '../types'

interface NotebookCardMenuProps {
  notebook: Notebook
  onDelete: (e: React.MouseEvent) => void
  isDeleting: boolean
  onClose: () => void
}

export default function NotebookCardMenu({ notebook, onDelete, isDeleting, onClose }: NotebookCardMenuProps) {
  return (
    <div className="absolute right-0 mt-1 w-40 rounded-lg border border-gray-100 bg-white shadow-lg z-10 py-1">
      <Link
        to={`/notes/${notebook.slug}/edit`}
        onClick={onClose}
        className="w-full flex items-center gap-2 px-3 py-2 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
      >
        <Settings className="h-3.5 w-3.5" />
        Settings
      </Link>
      <button
        type="button"
        onClick={onDelete}
        disabled={isDeleting}
        className="w-full flex items-center gap-2 px-3 py-2 text-xs text-red-600 hover:bg-red-50 transition-colors disabled:opacity-50"
      >
        <Trash2 className="h-3.5 w-3.5" />
        {isDeleting ? (
          <span className="flex items-center gap-1">
            <Loader2 className="h-3 w-3 animate-spin" />
            Deleting...
          </span>
        ) : (
          'Delete'
        )}
      </button>
    </div>
  )
}
