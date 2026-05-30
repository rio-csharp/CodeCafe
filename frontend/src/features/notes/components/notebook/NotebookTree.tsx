import { useState } from 'react'
import { Search, FolderOpen, Coffee, Archive } from 'lucide-react'
import type { TreeNode } from '../../utils/buildTree'
import type { Notebook } from '../../types'
import { useDebounce } from '../../../../hooks/useDebounce'
import { useNotebookItems } from '../../hooks/useNotesQueries'
import useTreeActions from '../../hooks/useTreeActions'
import TreeRootActions from './TreeRootActions'
import TreeContent from './TreeContent'

interface NotebookTreeProps {
  notebook: Notebook
  notebookSlug: string
  tree: TreeNode[]
  activePage: import('../../types').NotebookItem | null
  showArchived: boolean
  onShowArchivedChange: (value: boolean) => void
}

export default function NotebookTree({ notebook, notebookSlug, tree, activePage, showArchived, onShowArchivedChange }: NotebookTreeProps) {
  const canEdit = !!notebook.canEdit
  const [searchInput, setSearchInput] = useState('')
  const debouncedSearch = useDebounce(searchInput, 300)

  const {
    data: searchResults,
    isPending: searchPending,
    isError: searchError,
  } = useNotebookItems(notebook.id, debouncedSearch || undefined, showArchived)

  const isSearching = debouncedSearch.length > 0

  const {
    handleCreateRoot,
    handleCreateItem,
    handleRenameItem,
    handleArchiveItem,
    handleRestoreItem,
    handleDeleteItem,
    handleMoveUp,
    handleMoveDown,
    dragState,
  } = useTreeActions(notebook, tree)

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="px-5 pt-5 pb-3">
        <div className="flex items-center gap-3 mb-1">
          <div className="h-8 w-8 rounded-lg bg-stone-100 flex items-center justify-center shrink-0">
            <FolderOpen className="h-4 w-4 text-brand-brown" />
          </div>
          <h2 className="text-sm font-bold text-black leading-tight line-clamp-2">{notebook.title}</h2>
        </div>
        <p className="text-xs text-gray-400 ml-11">
          {notebook.visibility === 'public' ? 'Public Notebook' : 'Private Notebook'}
        </p>
      </div>

      {/* Search */}
      <div className="px-4 pb-3">
        <label className="relative block">
          <span className="sr-only">Search in this notebook</span>
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400" />
          <input
            type="text"
            placeholder="Search in this notebook..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="w-full pl-8 pr-3 py-2 rounded-lg border border-gray-100 bg-gray-50 text-xs outline-none focus:bg-white focus:border-gray-200 transition-colors placeholder:text-gray-400"
          />
        </label>
      </div>

      {/* Root actions */}
      {canEdit && !isSearching && (
        <div className="px-4 pb-2 flex items-center justify-between">
          <TreeRootActions onCreateRoot={handleCreateRoot} />
          <label className="flex items-center gap-1.5 text-xs text-gray-500 cursor-pointer select-none">
            <input
              type="checkbox"
              checked={showArchived}
              onChange={(e) => onShowArchivedChange(e.target.checked)}
              className="h-3 w-3 rounded border-gray-300 text-brand-brown focus:ring-brand-brown"
            />
            <Archive className="h-3 w-3" />
            <span>Show archived</span>
          </label>
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
          notebookSlug={notebookSlug}
          activePage={activePage}
          canEdit={canEdit}
          dragState={dragState}
          onMoveUp={handleMoveUp}
          onMoveDown={handleMoveDown}
          onCreateItem={handleCreateItem}
          onRenameItem={handleRenameItem}
          onArchiveItem={handleArchiveItem}
          onRestoreItem={handleRestoreItem}
          onDeleteItem={handleDeleteItem}
        />
      </div>

      {/* Footer */}
      <div className="px-5 py-3 border-t border-gray-100">
        <div className="flex items-center gap-2 text-xs text-gray-400">
          <Coffee className="h-3.5 w-3.5" />
          <span>Powered by CodeCafe</span>
        </div>
      </div>
    </div>
  )
}
