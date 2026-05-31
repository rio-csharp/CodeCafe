import { useState, useRef } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Star, MoreHorizontal, Edit3 } from 'lucide-react'
import { useLayout } from '@/app/LayoutContext'
import { useClickOutside } from '@/hooks/useClickOutside'
import TopBarActionMenu from './TopBarActionMenu'
import { useDeleteNotebook, useToggleFavorite } from '../../hooks/useNotesQueries'
import { useToast } from '@/components/ui/useToast'
import { getErrorMessage } from '@/lib/errorUtils'
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
        showToast(getErrorMessage(err, 'Failed to delete'), 'error')
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
          showToast(getErrorMessage(err, 'Failed to update favorite'), 'error')
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
            <TopBarActionMenu
              notebook={notebook}
              onCopyLink={handleCopyLink}
              onDelete={handleDelete}
              isDeleting={deleteNotebook.isPending}
              showDeleteConfirm={showDeleteConfirm}
              onShowDeleteConfirm={() => setShowDeleteConfirm(true)}
              onCancelDelete={() => setShowDeleteConfirm(false)}
              onClose={() => setMenuOpen(false)}
            />
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
