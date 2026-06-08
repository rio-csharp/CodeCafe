import { useState, useRef, useMemo, useCallback } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Search,
  FolderOpen,
  Coffee,
  Archive,
  Star,
  MoreHorizontal,
  Link2,
  Settings,
  Trash2,
  Loader2,
  ArrowLeft,
  Check,
  RefreshCw,
  FileUp,
} from 'lucide-react'
import type { TreeNode } from '@/entities/notebook'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import {
  NOTEBOOK_VISIBILITY_CONTEXT_LABELS,
  useNotebookItems,
} from '@/entities/notebook'
import { useDebounce } from '@/shared/hooks/useDebounce'
import { useTreeActions } from '@/features/manage-notebook-items'
import { useDeleteNotebook } from '@/features/delete-notebook'
import { useToggleFavorite } from '@/features/toggle-favorite'
import { ImportMarkdownModal } from '@/features/import-markdown'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { useClickOutside } from '@/shared/hooks/useClickOutside'
import { useLayout } from '@/shared/model/layoutContext'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'
import { TreeContext } from '../model/TreeContext'
import TreeRootActions from './TreeRootActions'
import TreeContent from './TreeContent'
import { useTranslation } from 'react-i18next'

interface NotebookTreeProps {
  notebook: Notebook
  notebookSlug: string
  tree: TreeNode[]
  activePage: NotebookItem | null
  showArchived: boolean
  onShowArchivedChange: (value: boolean) => void
  onRefreshNotebook?: () => void
}

function NotebookTreeComponent({ notebook, notebookSlug, tree, activePage, showArchived, onShowArchivedChange, onRefreshNotebook }: NotebookTreeProps) {
  const canEdit = !!notebook.canEdit
  const [searchInput, setSearchInput] = useState('')
  const debouncedSearch = useDebounce(searchInput, 300)
  const { t } = useTranslation()

  const isSearching = debouncedSearch.length > 0

  const {
    data: searchResults,
    isPending: searchPending,
    isError: searchError,
  } = useNotebookItems(notebook.id, debouncedSearch || undefined, showArchived, isSearching)

  const {
    handleCreateRoot,
    handleCreateItem,
    handleRenameItem,
    handleArchiveItem,
    handleRestoreItem,
    handleDeleteItem,
    dragState,
  } = useTreeActions(notebook, tree)

  const contextValue = useMemo(() => ({
    notebookSlug,
    activePath: activePage?.path ?? null,
    canEdit,
    dragState,
    onCreateItem: handleCreateItem,
    onRenameItem: handleRenameItem,
    onArchiveItem: handleArchiveItem,
    onRestoreItem: handleRestoreItem,
    onDeleteItem: handleDeleteItem,
  }), [notebookSlug, activePage?.path, canEdit, dragState, handleCreateItem, handleRenameItem, handleArchiveItem, handleRestoreItem, handleDeleteItem])

  // Notebook actions (moved from top bar)
  const { user } = useLayout()
  const navigate = useNavigate()
  const isAuthenticated = !!user
  const [menuOpen, setMenuOpen] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const [showImportModal, setShowImportModal] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const deleteNotebook = useDeleteNotebook()
  const toggleFavorite = useToggleFavorite()
  const { showToast } = useToast()

  useClickOutside(menuRef, () => {
    setMenuOpen(false)
    setShowDeleteConfirm(false)
  })

  const handleCopyLink = useCallback(() => {
    const url = `${window.location.origin}/notes/${notebook.slug}`
    navigator.clipboard.writeText(url).then(() => {
      showToast(t('notebook.linkCopied'))
    }).catch(() => {
      showToast(t('notebook.copyFailed'), 'error')
    })
    setMenuOpen(false)
  }, [notebook.slug, showToast, t])

  const handleDelete = useCallback(() => {
    deleteNotebook.mutate(notebook.id, {
      onSuccess: () => {
        showToast(t('notebook.deleted'))
        navigate('/notes')
      },
      onError: (err: unknown) => {
        showToast(getErrorMessage(err, t('notebook.deleteFailed')), 'error')
      },
    })
  }, [deleteNotebook, notebook.id, navigate, showToast, t])

  const handleToggleFavorite = useCallback(() => {
    if (!isAuthenticated) {
      navigate('/login')
      return
    }
    if (toggleFavorite.isPending) return
    toggleFavorite.mutate(
      { notebookId: notebook.id, isFavorited: notebook.isFavoritedByMe },
      {
        onError: (err: unknown) => {
          showToast(getErrorMessage(err, t('notebook.favoriteFailed')), 'error')
        },
      },
    )
  }, [isAuthenticated, navigate, notebook.id, notebook.isFavoritedByMe, showToast, t, toggleFavorite])

  return (
    <TreeContext.Provider value={contextValue}>
      <div className="flex flex-col h-full">
        {/* Header */}
        <div className="px-5 pt-5 pb-3">
          <div className="flex items-start gap-3">
            <div className="h-8 w-8 rounded-lg bg-surface-active flex items-center justify-center shrink-0 mt-0.5">
              <FolderOpen className="h-4 w-4 text-brand-brown" />
            </div>
            <div className="flex-1 min-w-0">
              <div className="flex items-start justify-between gap-2">
                <h2 className="text-sm font-bold text-text-primary leading-tight line-clamp-2">{notebook.title}</h2>
                <div className="flex items-center gap-1 shrink-0 mt-0.5">
                  <button
                    type="button"
                    onClick={handleToggleFavorite}
                    disabled={toggleFavorite.isPending}
                    className={`p-1 rounded-md transition-colors ${
                      notebook.isFavoritedByMe
                        ? 'text-status-favorite bg-status-favorite-bg'
                        : 'text-text-secondary hover:bg-surface-hover'
                    }`}
                    title={notebook.isFavoritedByMe ? t('notebook.favoriteRemove') : t('notebook.favoriteAdd')}
                    aria-label={notebook.isFavoritedByMe ? t('notebook.favoriteRemove') : t('notebook.favoriteAdd')}
                  >
                    <Star className={`h-3.5 w-3.5 ${notebook.isFavoritedByMe ? 'fill-status-favorite' : ''}`} />
                  </button>
                  <div className="relative" ref={menuRef}>
                    <button
                      type="button"
                      onClick={() => setMenuOpen(!menuOpen)}
                      className="p-1 text-text-secondary hover:bg-surface-hover rounded-md transition-colors"
                      aria-label="Notebook menu"
                    >
                      <MoreHorizontal className="h-3.5 w-3.5" />
                    </button>
                    {menuOpen && (
                      <div className="absolute right-0 mt-1 w-44 rounded-lg border border-border-subtle bg-surface shadow-lg z-50 py-1">
                        {onRefreshNotebook && (
                          <button
                            type="button"
                            onClick={() => {
                              onRefreshNotebook()
                              setMenuOpen(false)
                            }}
                            className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
                          >
                            <RefreshCw className="h-3.5 w-3.5" />
                            Refresh
                          </button>
                        )}
                        <button
                          type="button"
                          onClick={() => onShowArchivedChange(!showArchived)}
                          className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
                        >
                          <Archive className="h-3.5 w-3.5" />
                          <span className="flex-1 text-left">Show archived</span>
                          {showArchived && <Check className="h-3 w-3 text-brand-brown" />}
                        </button>
                        <button
                          type="button"
                          onClick={handleCopyLink}
                          className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
                        >
                          <Link2 className="h-3.5 w-3.5" />
                          Copy link
                        </button>
                        {notebook.canEdit && (
                          <>
                            <button
                              type="button"
                              onClick={() => {
                                setShowImportModal(true)
                                setMenuOpen(false)
                              }}
                              className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
                            >
                              <FileUp className="h-3.5 w-3.5" />
                              {t('notebook.importMarkdown')}
                            </button>
                            <div className="my-1 border-t border-border-subtle" />
                          </>
                        )}
                        {notebook.canEdit && (
                          <>
                            <Link
                              to={`/notes/${notebook.slug}/edit`}
                              onClick={() => setMenuOpen(false)}
                              className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
                            >
                              <Settings className="h-3.5 w-3.5" />
                              Notebook settings
                            </Link>
                            <div className="my-1 border-t border-border-subtle" />
                            {!showDeleteConfirm ? (
                              <button
                                type="button"
                                onClick={() => setShowDeleteConfirm(true)}
                                className="w-full flex items-center gap-2 px-3 py-2 text-xs text-status-error hover:bg-status-error-bg transition-colors"
                              >
                                <Trash2 className="h-3.5 w-3.5" />
                                Delete notebook
                              </button>
                            ) : (
                              <div className="px-3 py-2 space-y-2">
                                <p className="text-xs text-status-error">Are you sure? This cannot be undone.</p>
                                <div className="flex items-center gap-2">
                                  <button
                                    type="button"
                                    onClick={handleDelete}
                                    disabled={deleteNotebook.isPending}
                                    className="rounded-md bg-status-error px-2 py-1 text-xs font-medium text-text-inverse hover:bg-status-error-hover transition-colors disabled:opacity-50"
                                  >
                                    {deleteNotebook.isPending ? (
                                      <span className="flex items-center gap-1">
                                        <Loader2 className="h-3 w-3 animate-spin" />
                                        Deleting...
                                      </span>
                                    ) : (
                                      'Delete'
                                    )}
                                  </button>
                                  <button
                                    type="button"
                                    onClick={() => setShowDeleteConfirm(false)}
                                    className="rounded-md border border-border-default px-2 py-1 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
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
                </div>
              </div>
              <p className="text-xs text-text-tertiary mt-0.5">
                {NOTEBOOK_VISIBILITY_CONTEXT_LABELS[notebook.visibility]}
              </p>
            </div>
          </div>
        </div>

        {/* Search */}
        <div className="px-4 pb-3">
          <label className="relative block">
            <span className="sr-only">{t('notebook.search')}</span>
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-text-tertiary" />
            <input
              type="text"
              placeholder={t('notebook.search')}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              className="w-full pl-8 pr-3 py-2 rounded-lg border border-border-subtle bg-surface-hover text-xs outline-none focus:bg-surface focus:border-border-default transition-colors placeholder:text-text-tertiary text-text-primary"
            />
          </label>
        </div>

        {/* Root actions */}
        {canEdit && !isSearching && (
          <div className="px-4 pb-2">
            <TreeRootActions onCreateRoot={handleCreateRoot} />
          </div>
        )}

        {/* Tree or Search Results */}
        <div className="flex-1 overflow-y-auto px-2 pb-4">
          <TreeContent
            isSearching={isSearching}
            searchPending={searchPending}
            searchError={searchError}
            searchResults={searchResults}
            tree={tree}
          />
        </div>

        {/* Footer */}
        <div className="px-5 py-3 border-t border-border-subtle space-y-2">
          <Link
            to="/notes"
            className="flex items-center justify-center gap-1.5 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
          >
            <ArrowLeft className="h-3.5 w-3.5" />
            Back to Notes
          </Link>
          <div className="flex items-center gap-2 text-xs text-text-tertiary">
            <Coffee className="h-3.5 w-3.5" />
            <span>Browser + MCP ready</span>
          </div>
        </div>
      </div>

      <ImportMarkdownModal
        isOpen={showImportModal}
        onClose={() => setShowImportModal(false)}
        notebookSlug={notebook.slug}
        notebookId={notebook.id}
        tree={tree}
        onSuccess={onRefreshNotebook}
      />
    </TreeContext.Provider>
  )
}

export default function NotebookTree(props: NotebookTreeProps) {
  return (
    <ErrorBoundary fallback={<ErrorFallback title="Notebook Tree Error" description="The notebook tree failed to render. Try refreshing the page." />}>
      <NotebookTreeComponent {...props} />
    </ErrorBoundary>
  )
}
