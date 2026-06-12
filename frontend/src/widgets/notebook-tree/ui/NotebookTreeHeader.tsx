import { useState, useRef, useCallback } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Search,
  FolderOpen,
  Archive,
  MoreHorizontal,
  Link2,
  Settings,
  Trash2,
  Loader2,
  Check,
  RefreshCw,
  FileUp,
} from 'lucide-react'
import type { Notebook } from '@/entities/notebook'
import { useNotebookVisibilityContextLabels } from '@/entities/notebook'
import { useDeleteNotebook } from '@/features/delete-notebook'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { useClickOutside } from '@/shared/hooks/useClickOutside'
import { useTranslation } from 'react-i18next'
import { useTreeContext } from '../model/TreeContext'

interface NotebookTreeHeaderProps {
  notebook: Notebook
  showArchived: boolean
  onShowArchivedChange: (value: boolean) => void
  onRefreshNotebook?: () => void
  searchInput: string
  onSearchInputChange: (value: string) => void
  onOpenImportModal: () => void
}

export default function NotebookTreeHeader({
  notebook,
  showArchived,
  onShowArchivedChange,
  onRefreshNotebook,
  searchInput,
  onSearchInputChange,
  onOpenImportModal,
}: NotebookTreeHeaderProps) {
  const { t } = useTranslation()
  const visibilityContextLabels = useNotebookVisibilityContextLabels()
  const navigate = useNavigate()
  const { activePath } = useTreeContext()
  const [menuOpen, setMenuOpen] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const deleteNotebook = useDeleteNotebook()
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

  const handleOpenImportModal = useCallback(() => {
    onOpenImportModal()
    setMenuOpen(false)
  }, [onOpenImportModal])

  return (
    <>
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
                {onRefreshNotebook && (
                  <button
                    type="button"
                    onClick={onRefreshNotebook}
                    className="p-1 text-text-secondary hover:bg-surface-hover rounded-md transition-colors"
                    title={t('notebook.refresh')}
                    aria-label={t('notebook.refresh')}
                  >
                    <RefreshCw className="h-3.5 w-3.5" />
                  </button>
                )}
                <div className="relative" ref={menuRef}>
                  <button
                    type="button"
                    onClick={() => setMenuOpen(!menuOpen)}
                    className="p-1 text-text-secondary hover:bg-surface-hover rounded-md transition-colors"
                    aria-label={t('notebook.notebookMenu')}
                  >
                    <MoreHorizontal className="h-3.5 w-3.5" />
                  </button>
                  {menuOpen && (
                    <div className="absolute right-0 mt-1 w-44 rounded-lg border border-border-subtle bg-surface shadow-lg z-50 py-1">
                      <button
                        type="button"
                        onClick={() => onShowArchivedChange(!showArchived)}
                        className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
                      >
                        <Archive className="h-3.5 w-3.5" />
                        <span className="flex-1 text-left">{showArchived ? t('notebook.hideArchived') : t('notebook.showArchived')}</span>
                        {showArchived && <Check className="h-3 w-3 text-brand-brown" />}
                      </button>
                      <button
                        type="button"
                        onClick={handleCopyLink}
                        className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
                      >
                        <Link2 className="h-3.5 w-3.5" />
                        {t('notebook.copyLink')}
                      </button>
                      {notebook.canEdit && (
                        <>
                          <button
                            type="button"
                            onClick={handleOpenImportModal}
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
                            state={{ fromPagePath: activePath ?? undefined }}
                            onClick={() => setMenuOpen(false)}
                            className="w-full flex items-center gap-2 px-3 py-2 text-xs text-text-secondary hover:bg-surface-hover transition-colors"
                          >
                            <Settings className="h-3.5 w-3.5" />
                            {t('notebook.settingsMenu')}
                          </Link>
                          <div className="my-1 border-t border-border-subtle" />
                          {!showDeleteConfirm ? (
                            <button
                              type="button"
                              onClick={() => setShowDeleteConfirm(true)}
                              className="w-full flex items-center gap-2 px-3 py-2 text-xs text-status-error hover:bg-status-error-bg transition-colors"
                            >
                              <Trash2 className="h-3.5 w-3.5" />
                              {t('notebook.deleteNotebook')}
                            </button>
                          ) : (
                            <div className="px-3 py-2 space-y-2">
                              <p className="text-xs text-status-error">{t('notebook.deleteConfirmInline')}</p>
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
                                      {t('notebook.deleting')}
                                    </span>
                                  ) : (
                                    t('notebook.delete')
                                  )}
                                </button>
                                <button
                                  type="button"
                                  onClick={() => setShowDeleteConfirm(false)}
                                  className="rounded-md border border-border-default px-2 py-1 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                                >
                                  {t('notebook.cancel')}
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
              {visibilityContextLabels[notebook.visibility]}
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
            onChange={(e) => onSearchInputChange(e.target.value)}
            className="w-full pl-8 pr-3 py-2 rounded-lg border border-border-subtle bg-surface-hover text-xs outline-none focus:bg-surface focus:border-border-default transition-colors placeholder:text-text-tertiary text-text-primary"
          />
        </label>
      </div>
    </>
  )
}
