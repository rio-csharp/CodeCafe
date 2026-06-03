import { useState, useRef, createElement } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { MoreHorizontal, Star, FileText as PageIcon, Folder as FolderIcon } from 'lucide-react'
import { useLayout } from '@/shared/model/layoutContext'
import { useClickOutside } from '@/shared/hooks/useClickOutside'
import VisibilityBadge from './VisibilityBadge'
import NotebookCardMenu from './NotebookCardMenu'
import { useDeleteNotebook } from '@/features/delete-notebook'
import { useToggleFavorite } from '@/features/toggle-favorite'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { pickIcon, formatTimeAgo } from '@/shared/lib'
import type { Notebook } from '@/entities/notebook'

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
    <div className="group relative h-full" data-testid="notebook-card">
      <Link to={`/notes/${notebook.slug}`} className="flex flex-col h-full rounded-xl border border-border-default bg-surface p-5 transition-all hover:border-border-hover hover:shadow-sm">
        <div className="flex items-start gap-4">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-surface-active text-text-secondary">
            {createElement(iconComponent, { className: 'h-5 w-5' })}
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-text-primary truncate">{notebook.title}</h3>
              {showVisibility && <VisibilityBadge visibility={notebook.visibility} />}
            </div>
            <p className="mt-1 text-xs text-text-secondary line-clamp-2 leading-relaxed">{notebook.description || 'No description yet.'}</p>
          </div>
        </div>
        <div className="mt-3 flex items-center gap-3 text-[11px] text-text-tertiary">
          {hasItems ? (
            <>
              <span className="flex items-center gap-1"><PageIcon className="h-3 w-3" />{notebook.pageCount} page{notebook.pageCount !== 1 ? 's' : ''}</span>
              <span className="flex items-center gap-1"><FolderIcon className="h-3 w-3" />{notebook.folderCount} folder{notebook.folderCount !== 1 ? 's' : ''}</span>
            </>
          ) : (
            <span>No pages or folders yet</span>
          )}
        </div>
        <div className="mt-3 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="h-5 w-5 rounded-full bg-brand-brown flex items-center justify-center text-text-inverse text-[10px] font-medium">{initial}</div>
            <span className="text-xs text-text-secondary">{authorName}</span>
          </div>
          <span className="text-xs text-text-tertiary">{formatTimeAgo(lastActivity)}</span>
        </div>
      </Link>
      <div className="absolute top-3 right-3 flex items-center gap-1">
        <button
          type="button"
          onClick={handleToggleFavorite}
          disabled={toggleFavorite.isPending}
          className={`p-1.5 rounded-md transition-all ${notebook.isFavoritedByMe ? 'text-status-favorite bg-status-favorite-bg opacity-100' : 'text-text-tertiary hover:text-status-favorite hover:bg-status-favorite-bg opacity-0 group-hover:opacity-100'}`}
          title={notebook.isFavoritedByMe ? 'Remove from favorites' : 'Add to favorites'}
        >
          <Star className={`h-4 w-4 ${notebook.isFavoritedByMe ? 'fill-status-favorite' : ''}`} />
        </button>
        {notebook.canEdit && (
          <div ref={menuRef}>
            <button type="button" data-testid="notebook-menu-button" onClick={(e) => { e.preventDefault(); e.stopPropagation(); setMenuOpen(!menuOpen) }} className="p-1.5 rounded-md text-text-tertiary hover:text-text-secondary hover:bg-surface-hover opacity-0 group-hover:opacity-100 transition-all">
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
