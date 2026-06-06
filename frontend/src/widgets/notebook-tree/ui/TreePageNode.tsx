import { memo } from 'react'
import { Link } from 'react-router-dom'
import { FileText } from 'lucide-react'
import type { TreeNode } from '@/entities/notebook'
import { useTreeNodeActions } from '@/features/manage-notebook-items'
import { useTreeContext } from '../model/TreeContext'
import TreeRenameField from './TreeRenameField'
import TreeNodeActions from './TreeNodeActions'
import { TREE_INDENT_PER_LEVEL, TREE_INDENT_BASE } from '../lib/treeConstants'
import { useTranslation } from 'react-i18next'

interface TreePageNodeProps {
  node: TreeNode
  notebookSlug: string
  activePath: string | null
  level: number
}

function TreePageNode({
  node,
  notebookSlug,
  activePath,
  level,
}: TreePageNodeProps) {
  const { canEdit, dragState, onRenameItem, onArchiveItem, onRestoreItem, onDeleteItem } = useTreeContext()
  const { t } = useTranslation()
  const {
    isEditing,
    editTitle,
    setEditTitle,
    handleRename,
    handleArchive,
    handleRestore,
    handleDelete,
    handleKeyDown,
    startEditing,
    cancelEditing,
  } = useTreeNodeActions({ node, onRenameItem, onArchiveItem, onRestoreItem, onDeleteItem })

  const isActive = node.item.path === activePath
  const isDragging = dragState?.draggingId === node.item.id
  const paddingLeft = level * TREE_INDENT_PER_LEVEL + TREE_INDENT_BASE

  const handleDragStart = (e: React.DragEvent) => {
    if (!dragState || !canEdit) return
    e.dataTransfer.effectAllowed = 'move'
    e.dataTransfer.setData('text/plain', node.item.id)
    dragState.onDragStart(node.item.id)
  }

  const handleDragEnd = () => { dragState?.onDragEnd() }

  const icon = <FileText className="h-3.5 w-3.5 shrink-0 text-text-tertiary" />

  return (
    <div
      draggable={canEdit && !node.item.isArchived}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      data-tree-item-id={node.item.id}
      className={`group flex items-center gap-2 px-3 py-1.5 text-[13px] rounded-md transition-colors ${isActive ? 'bg-status-favorite-bg/60' : ''} ${isDragging ? 'opacity-40' : ''} ${node.item.isArchived ? 'opacity-60 italic' : ''}`}
      style={{ paddingLeft }}
    >
      {isEditing ? (
        // Edit mode: render a plain <div> instead of <Link>. The rename
        // input lives inside the same wrapper as the title, but the
        // surrounding row uses a React Router <Link> for navigation.
        // TreeRenameField's input has an onClick stopPropagation guard,
        // but a click landing on the input's padding, the wrapper, or
        // any of the rename buttons can still reach the <a> and trigger
        // a navigation — observed to refresh the page in the live app.
        // Easiest defense: don't render the <Link> at all when editing.
        <div className="flex items-center gap-2 flex-1 min-w-0 text-text-secondary">
          {icon}
          <TreeRenameField
            value={editTitle}
            onChange={setEditTitle}
            onConfirm={handleRename}
            onCancel={(e) => { e?.preventDefault(); cancelEditing() }}
            onKeyDown={handleKeyDown}
            ariaLabel={t('notebook.renamePage')}
          />
        </div>
      ) : (
        <Link
          to={`/notes/${notebookSlug}/${node.item.path}`}
          className={`flex items-center gap-2 flex-1 min-w-0 ${isActive ? 'text-brand-brown font-medium' : 'text-text-secondary hover:text-text-primary'}`}
        >
          {icon}
          <span className="truncate">{node.item.title}</span>
        </Link>
      )}

      <TreeNodeActions
        canEdit={canEdit}
        isEditing={isEditing}
        isArchived={node.item.isArchived}
        onRename={(e) => { e.preventDefault(); startEditing() }}
        onArchive={(e) => { e.preventDefault(); handleArchive() }}
        onRestore={(e) => { e.preventDefault(); handleRestore() }}
        onDelete={(e) => { e.preventDefault(); handleDelete() }}
      />
    </div>
  )
}

export default memo(TreePageNode)
