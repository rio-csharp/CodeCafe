import { useState, useRef, createElement } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { MoreHorizontal, Star, FileText as PageIcon, Folder as FolderIcon } from 'lucide-react'
import { useLayout } from '@/shared/model/layoutContext'
import { useClickOutside } from '@/shared/hooks/useClickOutside'
import { useConfirmDialog } from '@/shared/ui/ConfirmDialog'
import VisibilityBadge from './VisibilityBadge'
import NotebookCardMenu from './NotebookCardMenu'
import { useDeleteNotebook } from '@/features/delete-notebook'
import { useToggleFavorite } from '@/features/toggle-favorite'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { pickIcon, formatTimeAgo } from '@/shared/lib'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'
import type { Notebook } from '@/entities/notebook'
import { useTranslation } from 'react-i18next'

interface NotebookCardProps {
  notebook: Notebook
  showVisibility?: boolean
}

function NotebookCardComponent({ notebook, showVisibility = false }: NotebookCardProps) {
  const iconComponent = pickIcon(notebook.title)
  const { i18n, t } = useTranslation()
  const authorName = notebook.authorDisplayName || t('common.user')
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
  const { requestConfirm, confirmDialog } = useConfirmDialog()

  useClickOutside(menuRef, () => setMenuOpen(false))

  const handleDelete = async (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (!(await requestConfirm({ title: t('notebook.deleteConfirm', { title: notebook.title }), danger: true }))) return
    deleteNotebook.mutate(notebook.id, {
      onSuccess: () => { showToast(t('notebook.deleted')); setMenuOpen(false) },
      onError: (err: unknown) => {
        showToast(getErrorMessage(err, t('notebook.deleteFailed')), 'error')
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
          showToast(getErrorMessage(err, t('notebook.favoriteFailed')), 'error')
        },
      },
    )
  }

  const hasItems = notebook.itemCount > 0

  return (
    <div className="group relative h-full" data-testid="notebook-card">
      <Link to={`/notes/${notebook.slug}`} className="flex flex-col h-full rounded-xl border border-border-default bg-surface p-5 transition-all duration-200 hover:border-border-hover hover:shadow-md hover:-translate-y-0.5">
        <div className="flex items-start gap-4">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-surface-active text-text-secondary">
            {createElement(iconComponent, { className: 'h-5 w-5' })}
          </div>
          <div className="flex-1 min-w-0 pr-8">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-semibold text-text-primary truncate">{notebook.title}</h3>
              {showVisibility && <VisibilityBadge visibility={notebook.visibility} />}
            </div>
            <p className="mt-1 text-xs text-text-secondary line-clamp-2 leading-relaxed">{notebook.description || t('notebook.noDescription')}</p>
          </div>
        </div>
        <div className="mt-3 flex items-center gap-3 text-[11px] text-text-tertiary">
          {hasItems ? (
            <>
              <span className="flex items-center gap-1"><PageIcon className="h-3 w-3" />{notebook.pageCount} {t('notebook.pages', { count: notebook.pageCount })}</span>
              <span className="flex items-center gap-1"><FolderIcon className="h-3 w-3" />{notebook.folderCount} {t('notebook.folders', { count: notebook.folderCount })}</span>
            </>
          ) : (
            <span>{t('notebook.noItems')}</span>
          )}
        </div>
        <div className="mt-3 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="h-5 w-5 rounded-full bg-brand-brown-dark dark:bg-brand-brown flex items-center justify-center text-text-inverse text-[10px] font-medium">{initial}</div>
            <span className="text-xs text-text-secondary">{authorName}</span>
          </div>
          <span className="text-xs text-text-tertiary">{formatTimeAgo(lastActivity, i18n.language)}</span>
        </div>
      </Link>
      <div className="absolute top-3 right-3 flex items-center gap-1">
        <button
          type="button"
          onClick={handleToggleFavorite}
          disabled={toggleFavorite.isPending}
          className={`p-1.5 rounded-md transition-all ${notebook.isFavoritedByMe ? 'text-status-favorite bg-status-favorite-bg opacity-100' : 'text-text-tertiary hover:text-status-favorite hover:bg-status-favorite-bg opacity-100 sm:opacity-0 sm:group-hover:opacity-100 sm:group-focus-within:opacity-100'}`}
          title={notebook.isFavoritedByMe ? t('notebook.favoriteRemove') : t('notebook.favoriteAdd')}
          aria-label={notebook.isFavoritedByMe ? t('notebook.favoriteRemove') : t('notebook.favoriteAdd')}
          aria-pressed={notebook.isFavoritedByMe}
        >
          <Star className={`h-4 w-4 ${notebook.isFavoritedByMe ? 'fill-status-favorite' : ''}`} />
        </button>
        {notebook.canEdit && (
          <div ref={menuRef}>
            <button type="button" data-testid="notebook-menu-button" onClick={(e) => { e.preventDefault(); e.stopPropagation(); setMenuOpen(!menuOpen) }} className="p-1.5 rounded-md text-text-tertiary hover:text-text-secondary hover:bg-surface-hover opacity-100 sm:opacity-0 sm:group-hover:opacity-100 sm:group-focus-within:opacity-100 transition-all" title={t('notebook.notebookMenu')} aria-label={t('notebook.notebookMenu')} aria-expanded={menuOpen}>
              <MoreHorizontal className="h-4 w-4" />
            </button>
            {menuOpen && (
              <NotebookCardMenu notebook={notebook} onDelete={handleDelete} isDeleting={deleteNotebook.isPending} onClose={() => setMenuOpen(false)} />
            )}
          </div>
        )}
      </div>
      {confirmDialog}
    </div>
  )
}

export default function NotebookCard(props: NotebookCardProps) {
  return (
    <ErrorBoundary fallback={<ErrorFallback />}>
      <NotebookCardComponent {...props} />
    </ErrorBoundary>
  )
}
