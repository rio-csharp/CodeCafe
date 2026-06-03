import { useState } from 'react'
import { Loader2 } from 'lucide-react'
import type { TreeNode } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { useTreeContext } from '../model/TreeContext'
import TreeItem from './TreeItem'
import SearchResultItem from './SearchResultItem'
import { useTranslation } from 'react-i18next'

interface TreeContentProps {
  isSearching: boolean
  searchPending: boolean
  searchError: boolean
  searchResults: NotebookItem[] | undefined
  tree: TreeNode[]
}

export default function TreeContent({
  isSearching,
  searchPending,
  searchError,
  searchResults,
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
      return <p className="text-xs text-status-error text-center py-8">{t('errors.generic')}</p>
    }
    if (!searchResults?.length) {
      return <p className="text-xs text-text-tertiary text-center py-8">{t('errors.generic')}</p>
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
      {tree.map((node, idx) => (
        <TreeItem
          key={node.item.id}
          node={node}
          level={0}
          siblingCount={tree.length}
          index={idx}
        />
      ))}
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
