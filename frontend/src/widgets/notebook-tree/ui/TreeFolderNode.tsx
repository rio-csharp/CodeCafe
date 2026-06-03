import { useState } from 'react'
import { ChevronRight, Folder, FolderOpen } from 'lucide-react'
import type { TreeNode } from '@/entities/notebook'
import { useTreeNodeActions } from '@/features/manage-notebook-items'
import { useTreeContext } from '../model/TreeContext'
import TreeRenameField from './TreeRenameField'
import TreeNodeActions from './TreeNodeActions'
import TreeCreateMenu from './TreeCreateMenu'
import { TREE_INDENT_PER_LEVEL, TREE_INDENT_BASE } from '../lib/treeConstants'

interface TreeFolderNodeProps {
  node: TreeNode
  level: number
  siblingCount: number
  index: number
  children: React.ReactNode
}

export default function TreeFolderNode({
  node,
  level,
  siblingCount,
  index,
  children,
}: TreeFolderNodeProps) {
  const { canEdit, dragState, onCreateItem, onRenameItem, onArchiveItem, onRestoreItem, onDeleteItem, onMoveUp, onMoveDown } = useTreeContext()
  const [expanded, setExpanded] = useState(true)
  const [isDragOver, setIsDragOver] = useState(false)
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

  const isDragging = dragState?.draggingId === node.item.id
  const paddingLeft = level * TREE_INDENT_PER_LEVEL + TREE_INDENT_BASE

  const handleCreate = async (type: 'folder' | 'page') => {
    try {
      await onCreateItem(node.item.id, type)
      setExpanded(true)
    } catch {
      /* error handled by parent */
    }
  }

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
    dragState.onDropOnFolder(node.item.id)
  }

  return (
    <div
      draggable={canEdit && !node.item.isArchived}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      className={`${isDragging ? 'opacity-40' : ''} ${isDragOver ? 'bg-status-favorite-bg/60 rounded-md' : ''}`}
    >
      <div
        className={`group flex items-center gap-1 w-full text-left px-3 py-1.5 text-[13px] rounded-md transition-colors ${node.item.isArchived ? 'text-text-tertiary opacity-60 italic hover:bg-surface-hover' : 'text-text-secondary hover:bg-surface-hover'}`}
        style={{ paddingLeft }}
      >
        <button type="button" onClick={() => setExpanded(!expanded)} className="shrink-0 p-0.5">
          <ChevronRight className={`h-3.5 w-3.5 shrink-0 text-text-tertiary transition-transform ${expanded ? 'rotate-90' : ''}`} />
        </button>
        {expanded ? <FolderOpen className="h-4 w-4 shrink-0 text-brand-brown" /> : <Folder className="h-4 w-4 shrink-0 text-brand-brown" />}

        {isEditing ? (
          <TreeRenameField
            value={editTitle}
            onChange={setEditTitle}
            onConfirm={handleRename}
            onCancel={cancelEditing}
            onKeyDown={handleKeyDown}
            ariaLabel="Rename folder"
          />
        ) : (
          <span className="truncate font-medium flex-1 min-w-0">{node.item.title}</span>
        )}

        {canEdit && !isEditing && (
          <div className="hidden group-hover:flex items-center gap-0.5 shrink-0 ml-1">
            {!node.item.isArchived && (
              <TreeCreateMenu onCreateFolder={() => handleCreate('folder')} onCreatePage={() => handleCreate('page')} />
            )}
            <TreeNodeActions
              canEdit={canEdit}
              isEditing={isEditing}
              siblingCount={siblingCount}
              index={index}
              isArchived={node.item.isArchived}
              onMoveUp={(e) => { e.stopPropagation(); onMoveUp?.(node.item.id) }}
              onMoveDown={(e) => { e.stopPropagation(); onMoveDown?.(node.item.id) }}
              onRename={(e) => { e.stopPropagation(); startEditing() }}
              onArchive={(e) => { e.stopPropagation(); handleArchive() }}
              onRestore={(e) => { e.stopPropagation(); handleRestore() }}
              onDelete={(e) => { e.stopPropagation(); handleDelete() }}
            />
          </div>
        )}
      </div>
      {expanded && <div>{children}</div>}
    </div>
  )
}
