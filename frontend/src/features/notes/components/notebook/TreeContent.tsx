import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import type { TreeNode } from '../../utils/buildTree'
import type { NotebookItem } from '../../types'
import TreeItem from './TreeItem'
import SearchResultItem from './SearchResultItem'

interface TreeContentProps {
  isSearching: boolean
  searchPending: boolean
  searchError: boolean
  searchResults: NotebookItem[] | undefined
  tree: TreeNode[]
  notebookSlug: string
  activePage: NotebookItem | null
  canEdit: boolean
  dragState?: {
    draggingId: string | null
    onDragStart: (id: string) => void
    onDragEnd: () => void
    onDropOnFolder: (folderId: string) => void
    onDropOnRoot: () => void
  }
  onMoveUp: (itemId: string) => void
  onMoveDown: (itemId: string) => void
  onCreateItem: (parentId: string | null, type: 'folder' | 'page') => Promise<void>
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onArchiveItem: (itemId: string) => Promise<void>
  onRestoreItem: (itemId: string) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
}

export default function TreeContent({
  isSearching,
  searchPending,
  searchError,
  searchResults,
  tree,
  notebookSlug,
  activePage,
  canEdit,
  dragState,
  onMoveUp,
  onMoveDown,
  onCreateItem,
  onRenameItem,
  onArchiveItem,
  onRestoreItem,
  onDeleteItem,
}: TreeContentProps) {
  const [rootDragOver, setRootDragOver] = useState(false)

  if (isSearching) {
    if (searchPending) {
      return (
        <div className="flex items-center justify-center py-8">
          <Loader2 className="h-4 w-4 animate-spin text-gray-400" />
        </div>
      )
    }
    if (searchError) {
      return <p className="text-xs text-red-500 text-center py-8">Search failed. Please try again.</p>
    }
    if (!searchResults?.length) {
      return <p className="text-xs text-gray-400 text-center py-8">No results found.</p>
    }
    return (
      <div className="space-y-0.5">
        {searchResults.map((item) => (
          <SearchResultItem
            key={item.id}
            item={item}
            notebookSlug={notebookSlug}
            activePath={activePage?.path ?? null}
          />
        ))}
      </div>
    )
  }

  const handleRootDragOver = (e: React.DragEvent) => {
    if (!dragState || !canEdit) return
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    setRootDragOver(true)
  }

  const handleRootDragLeave = () => {
    setRootDragOver(false)
  }

  const handleRootDrop = (e: React.DragEvent) => {
    if (!dragState || !canEdit) return
    e.preventDefault()
    setRootDragOver(false)
    dragState.onDropOnRoot()
  }

  return (
    <div
      className={`space-y-0.5 ${rootDragOver ? 'bg-amber-50/30 rounded-md' : ''}`}
      onDragOver={handleRootDragOver}
      onDragLeave={handleRootDragLeave}
      onDrop={handleRootDrop}
    >
      {tree.map((node, idx) => (
        <TreeItem
          key={node.item.id}
          node={node}
          notebookSlug={notebookSlug}
          activePath={activePage?.path ?? null}
          level={0}
          canEdit={canEdit}
          onMoveUp={onMoveUp}
          onMoveDown={onMoveDown}
          siblingCount={tree.length}
          index={idx}
          dragState={dragState}
          onCreateItem={onCreateItem}
          onRenameItem={onRenameItem}
          onArchiveItem={onArchiveItem}
          onRestoreItem={onRestoreItem}
          onDeleteItem={onDeleteItem}
        />
      ))}
      {tree.length === 0 && (
        <div className="px-4 py-6 text-center">
          <p className="text-xs text-gray-400">This notebook is empty.</p>
          {canEdit && (
            <p className="text-xs text-gray-400 mt-1">Add a folder or page to get started.</p>
          )}
        </div>
      )}
    </div>
  )
}
