import { useState, useRef } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Star, MoreHorizontal, Edit3, Settings, Link2, Trash2 } from 'lucide-react'
import { useLayout } from '../../../../app/LayoutContext'
import { useClickOutside } from '../../../../hooks/useClickOutside'
import { useDeleteNotebook, useToggleFavorite } from '../../hooks/useNotesQueries'
import { useToast } from '../../../../components/ui/Toast'
import type { Notebook } from '../../types'

interface NotebookTopBarProps {
  notebook: Notebook
}

export default function NotebookTopBar({ notebook }: NotebookTopBarProps) {
  const { user } = useLayout()
  const navigate = useNavigate()
  const isAuthenticated = !!user
  const visibilityLabel = notebook.visibility === 'public' ? 'Public Notes' : 'My Notes'

  const [menuOpen, setMenuOpen] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const deleteNotebook = useDeleteNotebook()
  const toggleFavorite = useToggleFavorite()
  const { showToast } = useToast()

  useClickOutside(menuRef, () => {
    setMenuOpen(false)
    setShowDeleteConfirm(false)
  })

  const handleCopyLink = () => {
    const url = `${window.location.origin}/notes/${notebook.slug}`
    navigator.clipboard.writeText(url).then(() => {
      showToast('Link copied to clipboard')
    }).catch(() => {
      showToast('Failed to copy link', 'error')
    })
    setMenuOpen(false)
  }

  const handleDelete = () => {
    deleteNotebook.mutate(notebook.id, {
      onSuccess: () => {
        showToast('Notebook deleted')
        navigate('/notes')
      },
      onError: (err: unknown) => {
        const msg = err instanceof Error ? err.message : 'Failed to delete'
        showToast(msg, 'error')
      },
    })
  }

  const handleToggleFavorite = () => {
    if (!isAuthenticated) {
      navigate('/login')
      return
    }
    if (toggleFavorite.isPending) return
    toggleFavorite.mutate(
      { notebookId: notebook.id, isFavorited: notebook.isFavoritedByMe },
      {
        onError: (err: unknown) => {
          const msg = err instanceof Error ? err.message : 'Failed to update favorite'
          showToast(msg, 'error')
        },
      },
    )
  }

  return (
    <header className="h-14 border-b border-gray-100 bg-white flex items-center justify-between px-4 shrink-0">
      <nav className="flex items-center gap-2 min-w-0 text-sm">
        <Link to="/notes" className="text-gray-500 hover:text-black transition-colors shrink-0">
          Notes
        </Link>
        <span className="text-gray-300">/</span>
        <Link to="/notes" className="text-gray-500 hover:text-black transition-colors shrink-0">
          {visibilityLabel}
        </Link>
        <span className="text-gray-300">/</span>
        <span className="font-semibold text-black truncate">{notebook.title}</span>
      </nav>

      <div className="flex items-center gap-2 shrink-0">
        {/* Favorite */}
        <button
          onClick={handleToggleFavorite}
          disabled={toggleFavorite.isPending}
          className={`flex items-center gap-1 px-2 py-1 text-xs rounded-md transition-colors ${
            notebook.isFavoritedByMe
              ? 'text-amber-600 bg-amber-50'
              : 'text-gray-600 hover:bg-gray-50'
          }`}
          title={notebook.isFavoritedByMe ? 'Remove from favorites' : 'Add to favorites'}
        >
          <Star className={`h-3.5 w-3.5 ${notebook.isFavoritedByMe ? 'fill-amber-500' : ''}`} />
          <span>{notebook.favoriteCount}</span>
        </button>

        {/* More menu */}
        <div className="relative" ref={menuRef}>
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            className="p-1.5 text-gray-500 hover:bg-gray-50 rounded-md transition-colors"
          >
            <MoreHorizontal className="h-4 w-4" />
          </button>

          {menuOpen && (
            <div className="absolute right-0 mt-1.5 w-48 rounded-lg border border-gray-100 bg-white shadow-lg z-50 py-1">
              <button
                onClick={handleCopyLink}
                className="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 transition-colors"
              >
                <Link2 className="h-4 w-4" />
                Copy link
              </button>
              {notebook.canEdit && (
                <>
                  <Link
                    to={`/notes/${notebook.slug}/edit`}
                    onClick={() => setMenuOpen(false)}
                    className="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 transition-colors"
                  >
                    <Settings className="h-4 w-4" />
                    Notebook settings
                  </Link>
                  <div className="my-1 border-t border-gray-100" />
                  {!showDeleteConfirm ? (
                    <button
                      onClick={() => setShowDeleteConfirm(true)}
                      className="w-full flex items-center gap-2 px-3 py-2 text-sm text-red-600 hover:bg-red-50 transition-colors"
                    >
                      <Trash2 className="h-4 w-4" />
                      Delete notebook
                    </button>
                  ) : (
                    <div className="px-3 py-2 space-y-2">
                      <p className="text-xs text-red-600">Are you sure? This cannot be undone.</p>
                      <div className="flex items-center gap-2">
                        <button
                          onClick={handleDelete}
                          disabled={deleteNotebook.isPending}
                          className="rounded-md bg-red-600 px-2.5 py-1 text-xs font-medium text-white hover:bg-red-700 transition-colors disabled:opacity-50"
                        >
                          {deleteNotebook.isPending ? 'Deleting...' : 'Delete'}
                        </button>
                        <button
                          onClick={() => setShowDeleteConfirm(false)}
                          className="rounded-md border border-gray-200 px-2.5 py-1 text-xs font-medium text-gray-700 hover:bg-gray-50 transition-colors"
                        >
                          Cancel
                        </button>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </div>

        {notebook.canEdit ? (
          <Link
            to={`/notes/${notebook.slug}/edit`}
            className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-3 py-1.5 text-xs font-medium text-white hover:opacity-90 transition-opacity"
          >
            <Edit3 className="h-3 w-3" />
            Edit
          </Link>
        ) : !isAuthenticated ? (
          <Link
            to="/login"
            className="inline-flex items-center gap-1 rounded-lg bg-brand-brown px-3 py-1.5 text-xs font-medium text-white hover:opacity-90 transition-opacity"
          >
            Sign in to edit
          </Link>
        ) : null}
      </div>
    </header>
  )
}
