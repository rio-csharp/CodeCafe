import { useState, useRef, useCallback, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import {
  Archive,
  MoreHorizontal,
  Link2,
  Settings,
  Trash2,
  Loader2,
  Check,
  FileUp,
} from 'lucide-react'
import type { Notebook } from '@/entities/notebook'
import { useDeleteNotebook } from '@/features/delete-notebook'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { useClickOutside } from '@/shared/hooks/useClickOutside'
import { useTranslation } from 'react-i18next'
import { useTreeContext } from '../model/TreeContext'

interface NotebookTreeMenuProps {
  notebook: Notebook
  showArchived: boolean
  onShowArchivedChange: (value: boolean) => void
  onOpenImportModal: () => void
}

export default function NotebookTreeMenu({
  notebook,
  showArchived,
  onShowArchivedChange,
  onOpenImportModal,
}: NotebookTreeMenuProps) {
  const { t } = useTranslation()
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

  useEffect(() => {
    if (!menuOpen) return
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setMenuOpen(false)
        setShowDeleteConfirm(false)
      }
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [menuOpen])

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
    <div className="relative" ref={menuRef}>
      <button
        type="button"
        onClick={() => setMenuOpen(!menuOpen)}
        className="p-1 text-text-secondary hover:bg-surface-hover rounded-md transition-colors"
        aria-label={t('notebook.notebookMenu')}
        aria-haspopup="menu"
        aria-expanded={menuOpen}
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
  )
}
