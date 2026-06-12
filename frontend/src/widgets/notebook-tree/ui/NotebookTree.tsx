import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import {
  Coffee,
  ArrowLeft,
} from 'lucide-react'
import type { TreeNode } from '@/entities/notebook'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { useNotebookItems } from '@/entities/notebook'
import { useDebounce } from '@/shared/hooks/useDebounce'
import { useTreeActions } from '@/features/manage-notebook-items'
import { ImportMarkdownModal } from '@/features/import-markdown'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { ErrorFallback } from '@/shared/ui/ErrorBoundary'
import { TreeContext } from '../model/TreeContext'
import TreeRootActions from './TreeRootActions'
import TreeContent from './TreeContent'
import NotebookTreeHeader from './NotebookTreeHeader'

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
  const { t } = useTranslation()
  const canEdit = !!notebook.canEdit
  const [searchInput, setSearchInput] = useState('')
  const debouncedSearch = useDebounce(searchInput, 300)

  const isSearching = debouncedSearch.length > 0

  const {
    data: searchResults,
    isPending: searchPending,
    isError: searchError,
  } = useNotebookItems(notebook.id, debouncedSearch || undefined, showArchived, isSearching, false)

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

  const [showImportModal, setShowImportModal] = useState(false)

  return (
    <TreeContext.Provider value={contextValue}>
      <div className="flex flex-col h-full">
        <NotebookTreeHeader
          notebook={notebook}
          showArchived={showArchived}
          onShowArchivedChange={onShowArchivedChange}
          onRefreshNotebook={onRefreshNotebook}
          searchInput={searchInput}
          onSearchInputChange={setSearchInput}
          onOpenImportModal={() => setShowImportModal(true)}
        />

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
            {t('notebook.backToNotes')}
          </Link>
          <div className="flex items-center gap-2 text-xs text-text-tertiary">
            <Coffee className="h-3.5 w-3.5" />
            <span>{t('notebook.mcpReady')}</span>
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
    <ErrorBoundary fallback={<ErrorFallback />}>
      <NotebookTreeComponent {...props} />
    </ErrorBoundary>
  )
}
