import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import type { TreeNode } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import QueryError from '@/shared/ui/QueryError'
import { useTreeContext } from '../model/TreeContext'
import TreeItem from './TreeItem'
import SearchResultItem from './SearchResultItem'
import DropZone from './DropZone'
import { useTranslation } from 'react-i18next'

interface TreeContentProps {
  isSearching: boolean
  searchPending: boolean
  searchError: boolean
  searchResults: NotebookItem[] | undefined
  onSearchRetry: () => void
  tree: TreeNode[]
}

export default function TreeContent({
  isSearching,
  searchPending,
  searchError,
  searchResults,
  onSearchRetry,
  tree,
}: TreeContentProps) {
  const [rootDragOver, setRootDragOver] = useState(false)
  const { canEdit, dragState, notebookSlug, activePath } = useTreeContext()
  const { t } = useTranslation()

  if (isSearching) {
    if (searchPending) {
      return (
        <div className="flex items-center justify-center py-8">
          <Loader2 className="h-4 w-4 animate-spin text-text-tertiary" />
        </div>
      )
    }
    if (searchError) {
      return (
        <QueryError
          message={t('search.error')}
          onRetry={onSearchRetry}
          className="border-0 bg-transparent px-2 py-6"
        />
      )
    }
    if (!searchResults?.length) {
      return <p className="text-xs text-text-tertiary text-center py-8">{t('search.noResults')}</p>
    }
    return (
      <div className="space-y-0.5">
        {searchResults.map((item) => (
          <SearchResultItem
            key={item.id}
            item={item}
            notebookSlug={notebookSlug}
            activePath={activePath}
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
      className={`space-y-0.5 ${rootDragOver ? 'bg-status-favorite-bg/30 rounded-md' : ''}`}
      onDragOver={handleRootDragOver}
      onDragLeave={handleRootDragLeave}
      onDrop={handleRootDrop}
    >
      {tree.map((node) => (
        <TreeItem
          key={node.item.id}
          node={node}
          level={0}
        />
      ))}
      {tree.length > 0 && (
        <DropZone onDrop={() => dragState?.onDropReorder(tree[tree.length - 1].item.id, 'after')} />
      )}
      {tree.length === 0 && (
        <div className="px-4 py-6 text-center">
          <p className="text-xs text-text-tertiary">{t('notebook.noPages')}</p>
          {canEdit && (
            <p className="text-xs text-text-tertiary mt-1">{t('notebook.addPageHint')}</p>
          )}
        </div>
      )}
    </div>
  )
}
