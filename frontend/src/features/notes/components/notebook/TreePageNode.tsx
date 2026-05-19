import { Link } from 'react-router-dom'
import { FileText } from 'lucide-react'
import type { TreeNode } from '../../utils/buildTree'
import useTreeNodeActions from '../../hooks/useTreeNodeActions'
import TreeRenameField from './TreeRenameField'
import TreeNodeActions from './TreeNodeActions'

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
  }
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
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
  onDeleteItem,
}: TreePageNodeProps) {
  const {
    isEditing,
    editTitle,
    setEditTitle,
    handleRename,
    handleDelete,
    handleKeyDown,
    startEditing,
    cancelEditing,
  } = useTreeNodeActions({ node, onRenameItem, onDeleteItem })

  const isActive = node.item.path === activePath
  const isDragging = dragState?.draggingId === node.item.id
  const paddingLeft = level * 14 + 10

  const handleDragStart = (e: React.DragEvent) => {
    if (!dragState || !canEdit) return
    e.dataTransfer.effectAllowed = 'move'
    e.dataTransfer.setData('text/plain', node.item.id)
    dragState.onDragStart(node.item.id)
  }

  const handleDragEnd = () => { dragState?.onDragEnd() }

  return (
    <div
      draggable={canEdit}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      className={`group flex items-center gap-2 px-3 py-1.5 text-[13px] rounded-md transition-colors ${isActive ? 'bg-amber-50/60' : ''} ${isDragging ? 'opacity-40' : ''}`}
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
        onMoveUp={(e) => { e.preventDefault(); onMoveUp?.(node.item.id) }}
        onMoveDown={(e) => { e.preventDefault(); onMoveDown?.(node.item.id) }}
        onRename={(e) => { e.preventDefault(); startEditing() }}
        onDelete={(e) => { e.preventDefault(); handleDelete() }}
      />
    </div>
  )
}
