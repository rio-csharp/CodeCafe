import { useState, useRef } from 'react'
import { Link } from 'react-router-dom'
import {
  ChevronRight,
  Folder,
  FolderOpen,
  FileText,
  Plus,
  Pencil,
  Trash2,
  Check,
  X,
  ArrowUp,
  ArrowDown,
} from 'lucide-react'
import type { TreeNode } from '../../utils/buildTree'
import { useClickOutside } from '../../../../hooks/useClickOutside'

interface TreeItemProps {
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
  }
  onCreateItem: (parentId: string | null, type: 'folder' | 'page') => Promise<void>
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
}

export default function TreeItem({
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
  onCreateItem,
  onRenameItem,
  onDeleteItem,
}: TreeItemProps) {
  const [expanded, setExpanded] = useState(true)
  const [isEditing, setIsEditing] = useState(false)
  const [editTitle, setEditTitle] = useState(node.item.title)
  const [showCreateMenu, setShowCreateMenu] = useState(false)
  const [isDragOver, setIsDragOver] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useClickOutside(menuRef, () => setShowCreateMenu(false))

  const isFolder = node.item.type === 'folder'
  const isActive = node.item.type === 'page' && node.item.path === activePath
  const isDragging = dragState?.draggingId === node.item.id
  const paddingLeft = level * 14 + 10

  const handleCreate = async (type: 'folder' | 'page') => {
    try {
      await onCreateItem(isFolder ? node.item.id : node.item.parentId, type)
      setShowCreateMenu(false)
      setExpanded(true)
    } catch {
      /* error handled by parent */
    }
  }

  const handleRename = async () => {
    if (!editTitle.trim() || editTitle.trim() === node.item.title) {
      setIsEditing(false)
      setEditTitle(node.item.title)
      return
    }
    try {
      await onRenameItem(node.item.id, editTitle.trim(), node.item.sortOrder)
      setIsEditing(false)
    } catch {
      /* error handled by parent */
    }
  }

  const handleDelete = async () => {
    if (!confirm(`Delete "${node.item.title}"? This cannot be undone.`)) return
    try {
      await onDeleteItem(node.item.id)
    } catch {
      /* error handled by parent */
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleRename()
    if (e.key === 'Escape') {
      setIsEditing(false)
      setEditTitle(node.item.title)
    }
  }

  const handleDragStart = (e: React.DragEvent) => {
    if (!dragState || !canEdit) return
    e.dataTransfer.effectAllowed = 'move'
    e.dataTransfer.setData('text/plain', node.item.id)
    dragState.onDragStart(node.item.id)
  }

  const handleDragEnd = () => {
    dragState?.onDragEnd()
  }

  const handleDragOver = (e: React.DragEvent) => {
    if (!isFolder || !dragState || !canEdit) return
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    setIsDragOver(true)
  }

  const handleDragLeave = () => {
    setIsDragOver(false)
  }

  const handleDrop = (e: React.DragEvent) => {
    if (!isFolder || !dragState || !canEdit) return
    e.preventDefault()
    setIsDragOver(false)
    dragState.onDropOnFolder(node.item.id)
  }

  if (isFolder) {
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
          className="group flex items-center gap-1 w-full text-left px-3 py-1.5 text-[13px] text-gray-700 hover:bg-gray-50 rounded-md transition-colors"
          style={{ paddingLeft }}
        >
          <button onClick={() => setExpanded(!expanded)} className="shrink-0 p-0.5">
            <ChevronRight
              className={`h-3.5 w-3.5 shrink-0 text-gray-400 transition-transform ${expanded ? 'rotate-90' : ''}`}
            />
          </button>
          {expanded ? (
            <FolderOpen className="h-4 w-4 shrink-0 text-brand-brown" />
          ) : (
            <Folder className="h-4 w-4 shrink-0 text-brand-brown" />
          )}

          {isEditing ? (
            <div className="flex items-center gap-1 flex-1 min-w-0">
              <input
                aria-label="Rename folder"
                value={editTitle}
                onChange={(e) => setEditTitle(e.target.value)}
                onKeyDown={handleKeyDown}
                autoFocus
                className="flex-1 min-w-0 bg-white border border-gray-200 rounded px-1.5 py-0.5 text-[13px] outline-none focus:border-brand-brown"
                onClick={(e) => e.stopPropagation()}
              />
              <button onClick={handleRename} className="p-0.5 text-green-600 hover:text-green-700">
                <Check className="h-3.5 w-3.5" />
              </button>
              <button
                onClick={() => {
                  setIsEditing(false)
                  setEditTitle(node.item.title)
                }}
                className="p-0.5 text-gray-400 hover:text-gray-600"
              >
                <X className="h-3.5 w-3.5" />
              </button>
            </div>
          ) : (
            <span className="truncate font-medium flex-1 min-w-0">{node.item.title}</span>
          )}

          {canEdit && !isEditing && (
            <div className="hidden group-hover:flex items-center gap-0.5 shrink-0 ml-1">
              {siblingCount > 1 && (
                <>
                  <button
                    onClick={(e) => {
                      e.stopPropagation()
                      onMoveUp?.(node.item.id)
                    }}
                    disabled={index === 0}
                    className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors disabled:opacity-30"
                    title="Move up"
                  >
                    <ArrowUp className="h-3 w-3" />
                  </button>
                  <button
                    onClick={(e) => {
                      e.stopPropagation()
                      onMoveDown?.(node.item.id)
                    }}
                    disabled={index === siblingCount - 1}
                    className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors disabled:opacity-30"
                    title="Move down"
                  >
                    <ArrowDown className="h-3 w-3" />
                  </button>
                </>
              )}
              <div className="relative" ref={menuRef}>
                <button
                  onClick={(e) => {
                    e.stopPropagation()
                    setShowCreateMenu(!showCreateMenu)
                  }}
                  className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors"
                  title="Add item"
                >
                  <Plus className="h-3.5 w-3.5" />
                </button>
                {showCreateMenu && (
                  <div className="absolute left-0 top-full mt-1 w-36 rounded-lg border border-gray-100 bg-white shadow-lg z-50 py-1">
                    <button
                      onClick={() => handleCreate('folder')}
                      className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
                    >
                      <Folder className="h-3.5 w-3.5 text-brand-brown" />
                      New folder
                    </button>
                    <button
                      onClick={() => handleCreate('page')}
                      className="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-gray-700 hover:bg-gray-50 transition-colors"
                    >
                      <FileText className="h-3.5 w-3.5 text-gray-400" />
                      New page
                    </button>
                  </div>
                )}
              </div>

              <button
                onClick={(e) => {
                  e.stopPropagation()
                  setIsEditing(true)
                }}
                className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors"
                title="Rename"
              >
                <Pencil className="h-3 w-3" />
              </button>

              <button
                onClick={(e) => {
                  e.stopPropagation()
                  handleDelete()
                }}
                className="p-0.5 text-gray-400 hover:text-red-600 rounded transition-colors"
                title="Delete"
              >
                <Trash2 className="h-3 w-3" />
              </button>
            </div>
          )}
        </div>
        {expanded && (
          <div>
            {node.children.map((child, childIndex) => (
              <TreeItem
                key={child.item.id}
                node={child}
                notebookSlug={notebookSlug}
                activePath={activePath}
                level={level + 1}
                canEdit={canEdit}
                onMoveUp={onMoveUp}
                onMoveDown={onMoveDown}
                siblingCount={node.children.length}
                index={childIndex}
                dragState={dragState}
                onCreateItem={onCreateItem}
                onRenameItem={onRenameItem}
                onDeleteItem={onDeleteItem}
              />
            ))}
          </div>
        )}
      </div>
    )
  }

  // Page node
  return (
    <div
      draggable={canEdit}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      className={`group flex items-center gap-2 px-3 py-1.5 text-[13px] rounded-md transition-colors ${
        isActive ? 'bg-amber-50/60' : ''
      } ${isDragging ? 'opacity-40' : ''}`}
      style={{ paddingLeft }}
    >
      <Link
        to={`/notes/${notebookSlug}/${node.item.path}`}
        className={`flex items-center gap-2 flex-1 min-w-0 ${
          isActive ? 'text-brand-brown font-medium' : 'text-gray-600 hover:text-black'
        }`}
      >
        <FileText className="h-3.5 w-3.5 shrink-0 text-gray-400" />
        {isEditing ? (
          <div className="flex items-center gap-1 flex-1 min-w-0">
            <input
              aria-label="Rename page"
              value={editTitle}
              onChange={(e) => setEditTitle(e.target.value)}
              onKeyDown={handleKeyDown}
              autoFocus
              className="flex-1 min-w-0 bg-white border border-gray-200 rounded px-1.5 py-0.5 text-[13px] outline-none focus:border-brand-brown"
              onClick={(e) => e.stopPropagation()}
            />
            <button onClick={handleRename} className="p-0.5 text-green-600 hover:text-green-700">
              <Check className="h-3.5 w-3.5" />
            </button>
            <button
              onClick={(e) => {
                e.preventDefault()
                setIsEditing(false)
                setEditTitle(node.item.title)
              }}
              className="p-0.5 text-gray-400 hover:text-gray-600"
            >
              <X className="h-3.5 w-3.5" />
            </button>
          </div>
        ) : (
          <span className="truncate">{node.item.title}</span>
        )}
      </Link>

      {canEdit && !isEditing && (
        <div className="hidden group-hover:flex items-center gap-0.5 shrink-0 ml-1">
          {siblingCount > 1 && (
            <>
              <button
                onClick={(e) => {
                  e.preventDefault()
                  onMoveUp?.(node.item.id)
                }}
                disabled={index === 0}
                className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors disabled:opacity-30"
                title="Move up"
              >
                <ArrowUp className="h-3 w-3" />
              </button>
              <button
                onClick={(e) => {
                  e.preventDefault()
                  onMoveDown?.(node.item.id)
                }}
                disabled={index === siblingCount - 1}
                className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors disabled:opacity-30"
                title="Move down"
              >
                <ArrowDown className="h-3 w-3" />
              </button>
            </>
          )}
          <button
            onClick={(e) => {
              e.preventDefault()
              setIsEditing(true)
            }}
            className="p-0.5 text-gray-400 hover:text-brand-brown rounded transition-colors"
            title="Rename"
          >
            <Pencil className="h-3 w-3" />
          </button>
          <button
            onClick={(e) => {
              e.preventDefault()
              handleDelete()
            }}
            className="p-0.5 text-gray-400 hover:text-red-600 rounded transition-colors"
            title="Delete"
          >
            <Trash2 className="h-3 w-3" />
          </button>
        </div>
      )}
    </div>
  )
}
