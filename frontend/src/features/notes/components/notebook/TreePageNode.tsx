import { useState } from 'react'
import { Link } from 'react-router-dom'
import { FileText } from 'lucide-react'
import type { TreeNode } from '../../utils/buildTree'
import useTreeNodeActions from '../../hooks/useTreeNodeActions'
import TreeRenameField from './TreeRenameField'
import TreeNodeActions from './TreeNodeActions'
import { TREE_INDENT_PER_LEVEL, TREE_INDENT_BASE } from './treeConstants'

interface TreePageNodeProps {
  node: TreeNode
  notebookSlug: string
  activePath: string | null
  level: number
  canEdit: boolean
  onMoveUp?: (itemId: string) => void
  onMoveDown?: (itemId: string) => void
  siblingCount: number
  index: number
  dragState?: {
    draggingId: string | null
    onDragStart: (id: string) => void
    onDragEnd: () => void
    onDropOnFolder: (folderId: string) => void
    onDropOnRoot: () => void
  }
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onArchiveItem: (itemId: string) => Promise<void>
  onRestoreItem: (itemId: string) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
}

export default function TreePageNode({
  node,
  notebookSlug,
  activePath,
  level,
  canEdit,
  onMoveUp,
  onMoveDown,
  siblingCount,
  index,
  dragState,
  onRenameItem,
  onArchiveItem,
  onRestoreItem,
  onDeleteItem,
}: TreePageNodeProps) {
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

  const [isDragOver, setIsDragOver] = useState(false)

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

  const handleDragOver = (e: React.DragEvent) => {
    if (!dragState || !canEdit || node.item.isArchived) return
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    setIsDragOver(true)
  }

  const handleDragLeave = () => { setIsDragOver(false) }

  const handleDrop = (e: React.DragEvent) => {
    if (!dragState || !canEdit || node.item.isArchived) return
    e.preventDefault()
    e.stopPropagation()
    setIsDragOver(false)
    if (node.item.parentId) {
      dragState.onDropOnFolder(node.item.parentId)
    } else {
      dragState.onDropOnRoot()
    }
  }

  return (
    <div
      draggable={canEdit && !node.item.isArchived}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      className={`group flex items-center gap-2 px-3 py-1.5 text-[13px] rounded-md transition-colors ${isActive ? 'bg-amber-50/60' : ''} ${isDragging ? 'opacity-40' : ''} ${isDragOver ? 'bg-amber-50/60' : ''} ${node.item.isArchived ? 'opacity-60 italic' : ''}`}
      style={{ paddingLeft }}
    >
      <Link
        to={`/notes/${notebookSlug}/${node.item.path}`}
        className={`flex items-center gap-2 flex-1 min-w-0 ${isActive ? 'text-brand-brown font-medium' : 'text-gray-600 hover:text-black'}`}
      >
        <FileText className="h-3.5 w-3.5 shrink-0 text-gray-400" />
        {isEditing ? (
          <TreeRenameField
            value={editTitle}
            onChange={setEditTitle}
            onConfirm={handleRename}
            onCancel={(e) => { e?.preventDefault(); cancelEditing() }}
            onKeyDown={handleKeyDown}
            ariaLabel="Rename page"
          />
        ) : (
          <span className="truncate">{node.item.title}</span>
        )}
      </Link>

      <TreeNodeActions
        canEdit={canEdit}
        isEditing={isEditing}
        siblingCount={siblingCount}
        index={index}
        isArchived={node.item.isArchived}
        onMoveUp={(e) => { e.preventDefault(); onMoveUp?.(node.item.id) }}
        onMoveDown={(e) => { e.preventDefault(); onMoveDown?.(node.item.id) }}
        onRename={(e) => { e.preventDefault(); startEditing() }}
        onArchive={(e) => { e.preventDefault(); handleArchive() }}
        onRestore={(e) => { e.preventDefault(); handleRestore() }}
        onDelete={(e) => { e.preventDefault(); handleDelete() }}
      />
    </div>
  )
}
