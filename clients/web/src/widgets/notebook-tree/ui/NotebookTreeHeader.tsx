import {
  Search,
  FolderOpen,
  RefreshCw,
} from 'lucide-react'
import type { Notebook } from '@/entities/notebook'
import { useNotebookVisibilityContextLabels } from '@/entities/notebook'
import { useTranslation } from 'react-i18next'
import NotebookTreeMenu from './NotebookTreeMenu'

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
                <NotebookTreeMenu
                  notebook={notebook}
                  showArchived={showArchived}
                  onShowArchivedChange={onShowArchivedChange}
                  onOpenImportModal={onOpenImportModal}
                />
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
