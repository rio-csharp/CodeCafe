import { Edit3, Link as LinkIcon, Maximize2, Minimize2, RefreshCw } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useToast } from '@/shared/ui/Toast'
import type { NotebookItem } from '@/entities/notebook-item'

interface NotebookReaderChromeProps {
  activePage: NotebookItem
  isEditingPage: boolean
  isFullWidth: boolean
  isFetching: boolean
  canEdit: boolean
  onToggleFullWidth: () => void
  onRefresh: () => void
  onEdit: () => void
  onCopyLink?: () => void
  onCopyTitle?: () => void
}

export default function NotebookReaderChrome({
  activePage,
  isEditingPage,
  isFullWidth,
  isFetching,
  canEdit,
  onToggleFullWidth,
  onRefresh,
  onEdit,
  onCopyLink,
  onCopyTitle,
}: NotebookReaderChromeProps) {
  const { t } = useTranslation()
  const { showToast } = useToast()

  const handleCopyTitle = () => {
    if (onCopyTitle) {
      onCopyTitle()
      return
    }
    navigator.clipboard.writeText(activePage.title)
      .then(() => showToast(t('notebook.titleCopied')))
      .catch(() => showToast(t('notebook.copyFailed'), 'error'))
  }

  const handleCopyLink = () => {
    if (onCopyLink) {
      onCopyLink()
      return
    }
    navigator.clipboard.writeText(window.location.href)
      .then(() => showToast(t('notebook.linkCopied')))
      .catch(() => showToast(t('notebook.copyFailed'), 'error'))
  }

  return (
    <div className="sticky top-0 z-10 -mx-4 sm:-mx-6 lg:-mx-12 px-4 sm:px-6 lg:px-12 flex items-start justify-between gap-4 py-1.5 bg-surface/95 backdrop-blur-sm">
      {!isEditingPage && (
        <h1
          className="text-xl font-semibold text-text-primary truncate min-w-0 cursor-pointer"
          title={activePage.title}
          onClick={handleCopyTitle}
        >
          {activePage.title}
        </h1>
      )}
      <div className={`flex items-center gap-2 shrink-0 ${isEditingPage ? '' : 'mt-0.5'}`}>
        {!isEditingPage && (
          <>
            <button
              type="button"
              onClick={onToggleFullWidth}
              className="inline-flex items-center gap-1 rounded-md border border-border-subtle px-2 py-0.5 text-[11px] text-text-secondary hover:bg-surface-hover transition-colors"
              title={isFullWidth ? t('notebook.collapseWidth') : t('notebook.expandWidth')}
            >
              {isFullWidth ? <Minimize2 className="h-3 w-3" /> : <Maximize2 className="h-3 w-3" />}
              {isFullWidth ? t('notebook.collapse') : t('notebook.fullWidth')}
            </button>
            <button
              type="button"
              onClick={onRefresh}
              disabled={isFetching}
              className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors disabled:opacity-50"
              title={t('notebook.refreshContent')}
            >
              <RefreshCw className={`h-3 w-3 ${isFetching ? 'animate-spin' : ''}`} />
              {t('notebook.refresh')}
            </button>
            <button
              type="button"
              onClick={handleCopyLink}
              className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
              title={t('notebook.copyLink')}
            >
              <LinkIcon className="h-3 w-3" />
              {t('notebook.copyLink')}
            </button>
          </>
        )}
        {!isEditingPage && canEdit && !activePage.isArchived && (
          <button
            type="button"
            onClick={onEdit}
            aria-label={t('notebook.editPage')}
            className="inline-flex items-center gap-1 rounded-lg border border-border-default px-3 py-1.5 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
          >
            <Edit3 className="h-3 w-3" />
            {t('notebook.editPage')}
          </button>
        )}
      </div>
    </div>
  )
}
