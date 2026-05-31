import { useState, useRef, createElement } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { MoreHorizontal, Star, FileText as PageIcon, Folder as FolderIcon } from 'lucide-react'
import { useLayout } from '@/app/LayoutContext'
import { useClickOutside } from '@/hooks/useClickOutside'
import VisibilityBadge from './VisibilityBadge'
import NotebookCardMenu from './NotebookCardMenu'
import { useDeleteNotebook, useToggleFavorite } from '../hooks/useNotesQueries'
import { useToast } from '@/components/ui/useToast'
import { getErrorMessage } from '@/lib/errorUtils'
import pickIcon from '../utils/pickIcon'
import timeAgo from '../utils/timeAgo'
import type { Notebook } from '../types'

interface NotebookCardProps {
  notebook: Notebook
  showVisibility?: boolean
}

export default function NotebookCard({ notebook, showVisibility = false }: NotebookCardProps) {
  const iconComponent = pickIcon(notebook.title)
  const authorName = notebook.authorDisplayName || 'User'
  const initial = authorName.charAt(0).toUpperCase()
  const lastActivity = notebook.lastActivityAtUtc || notebook.updatedAtUtc || new Date().toISOString()
  const [menuOpen, setMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const navigate = useNavigate()
  const { user } = useLayout()
  const isAuthenticated = !!user
  const deleteNotebook = useDeleteNotebook()
  const toggleFavorite = useToggleFavorite()
  const { showToast } = useToast()

  useClickOutside(menuRef, () => setMenuOpen(false))

  const handleDelete = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (!confirm(`Delete "${notebook.title}"? This cannot be undone.`)) return
    deleteNotebook.mutate(notebook.id, {
      onSuccess: () => { showToast('Notebook deleted'); setMenuOpen(false) },
      onError: (err: unknown) => {
        showToast(getErrorMessage(err, 'Failed to delete'), 'error')
      },
    })
  }

  const handleToggleFavorite = (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (!isAuthenticated) { navigate("/login"); return }
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

  const hasItems = notebook.itemCount > 0

  return (
    <div className="group relative h-full">
      <Link to={`/notes/${notebook.slug}`} className="flex flex-col h-full rounded-xl border border-gray-200 bg-white p-5 transition-all hover:border-gray-300 hover:shadow-sm">
        <div className="flex items-start gap-4">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-stone-100 text-stone-600">
            {createElement(iconComponent, { className: 'h-5 w-5' })}
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-black truncate">{notebook.title}</h3>
              {showVisibility && <VisibilityBadge visibility={notebook.visibility} />}
            </div>
            <p className="mt-1 text-xs text-gray-500 line-clamp-2 leading-relaxed">{notebook.description || 'No description'}</p>
          </div>
        </div>
        <div className="mt-3 flex items-center gap-3 text-[11px] text-gray-400">
          {hasItems ? (
            <>
              <span className="flex items-center gap-1"><PageIcon className="h-3 w-3" />{notebook.pageCount} page{notebook.pageCount !== 1 ? 's' : ''}</span>
              <span className="flex items-center gap-1"><FolderIcon className="h-3 w-3" />{notebook.folderCount} folder{notebook.folderCount !== 1 ? 's' : ''}</span>
            </>
          ) : (
            <span>Empty notebook</span>
          )}
        </div>
        <div className="mt-3 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="h-5 w-5 rounded-full bg-brand-brown flex items-center justify-center text-white text-[10px] font-medium">{initial}</div>
            <span className="text-xs text-gray-500">{authorName}</span>
          </div>
          <span className="text-xs text-gray-400">{timeAgo(lastActivity)}</span>
        </div>
      </Link>
      <div className="absolute top-3 right-3 flex items-center gap-1">
        <button
          type="button"
          onClick={handleToggleFavorite}
          disabled={toggleFavorite.isPending}
          className={`p-1.5 rounded-md transition-all ${notebook.isFavoritedByMe ? 'text-amber-500 bg-amber-50 opacity-100' : 'text-gray-400 hover:text-amber-500 hover:bg-amber-50 opacity-0 group-hover:opacity-100'}`}
          title={notebook.isFavoritedByMe ? 'Remove from favorites' : 'Add to favorites'}
        >
          <Star className={`h-4 w-4 ${notebook.isFavoritedByMe ? 'fill-amber-500' : ''}`} />
        </button>
        {notebook.canEdit && (
          <div ref={menuRef}>
            <button type="button" onClick={(e) => { e.preventDefault(); e.stopPropagation(); setMenuOpen(!menuOpen) }} className="p-1.5 rounded-md text-gray-400 hover:text-gray-700 hover:bg-gray-50 opacity-0 group-hover:opacity-100 transition-all">
              <MoreHorizontal className="h-4 w-4" />
            </button>
            {menuOpen && (
              <NotebookCardMenu notebook={notebook} onDelete={handleDelete} isDeleting={deleteNotebook.isPending} onClose={() => setMenuOpen(false)} />
            )}
          </div>
        )}
      </div>
    </div>
  )
}
