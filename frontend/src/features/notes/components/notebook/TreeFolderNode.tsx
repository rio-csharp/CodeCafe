import { useState } from 'react'
import { ChevronRight, Folder, FolderOpen } from 'lucide-react'
import type { TreeNode } from '../../utils/buildTree'
import useTreeNodeActions from '../../hooks/useTreeNodeActions'
import TreeRenameField from './TreeRenameField'
import TreeNodeActions from './TreeNodeActions'
import TreeCreateMenu from './TreeCreateMenu'

interface TreeFolderNodeProps {
  node: TreeNode
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
  }
  onCreateItem: (parentId: string | null, type: 'folder' | 'page') => Promise<void>
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onArchiveItem: (itemId: string) => Promise<void>
  onRestoreItem: (itemId: string) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
  children: React.ReactNode
}

export default function TreeFolderNode({
  node,
  level,
  canEdit,
  onMoveUp,
  onMoveDown,
  siblingCount,
  index,
  dragState,
  onCreateItem,
  onRenameItem,
  onArchiveItem,
  onRestoreItem,
  onDeleteItem,
  children,
}: TreeFolderNodeProps) {
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
  const paddingLeft = level * 14 + 10

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
    if (!dragState || !canEdit) return
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    setIsDragOver(true)
  }

  const handleDragLeave = () => { setIsDragOver(false) }

  const handleDrop = (e: React.DragEvent) => {
    if (!dragState || !canEdit) return
    e.preventDefault()
    setIsDragOver(false)
    dragState.onDropOnFolder(node.item.id)
  }

  return (
    <div
      draggable={canEdit}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      className={`${isDragging ? 'opacity-40' : ''} ${isDragOver ? 'bg-amber-50/60 rounded-md' : ''}`}
    >
      <div
        className={`group flex items-center gap-1 w-full text-left px-3 py-1.5 text-[13px] rounded-md transition-colors ${node.item.isArchived ? 'text-gray-400 opacity-60 italic hover:bg-gray-50' : 'text-gray-700 hover:bg-gray-50'}`}
        style={{ paddingLeft }}
      >
        <button type="button" onClick={() => setExpanded(!expanded)} className="shrink-0 p-0.5">
          <ChevronRight className={`h-3.5 w-3.5 shrink-0 text-gray-400 transition-transform ${expanded ? 'rotate-90' : ''}`} />
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
