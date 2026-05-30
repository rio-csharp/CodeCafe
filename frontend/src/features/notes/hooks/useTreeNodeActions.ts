import { useState, useCallback } from 'react'
import type { TreeNode } from '../utils/buildTree'
import { useToast } from '../../../components/ui/useToast'

interface UseTreeNodeActionsOptions {
  node: TreeNode
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onArchiveItem: (itemId: string) => Promise<void>
  onRestoreItem: (itemId: string) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
}

export default function useTreeNodeActions({ node, onRenameItem, onArchiveItem, onRestoreItem, onDeleteItem }: UseTreeNodeActionsOptions) {
  const [isEditing, setIsEditing] = useState(false)
  const [editTitle, setEditTitle] = useState(node.item.title)
  const { showToast } = useToast()

  const handleRename = useCallback(async () => {
    if (!editTitle.trim() || editTitle.trim() === node.item.title) {
      setIsEditing(false)
      setEditTitle(node.item.title)
      return
    }
    try {
      await onRenameItem(node.item.id, editTitle.trim(), node.item.sortOrder)
      setIsEditing(false)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to rename'
      showToast(message, 'error')
    }
  }, [editTitle, node.item.id, node.item.title, node.item.sortOrder, onRenameItem, showToast])

  const handleArchive = useCallback(async () => {
    if (!confirm(`Archive "${node.item.title}"? It will be hidden from the notebook.`)) return
    try {
      await onArchiveItem(node.item.id)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to archive'
      showToast(message, 'error')
    }
  }, [node.item.id, node.item.title, onArchiveItem, showToast])

  const handleRestore = useCallback(async () => {
    try {
      await onRestoreItem(node.item.id)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to restore'
      showToast(message, 'error')
    }
  }, [node.item.id, onRestoreItem, showToast])

  const handleDelete = useCallback(async () => {
    const isArchived = node.item.isArchived
    const action = isArchived ? 'permanently delete' : 'archive and delete'
    if (!confirm(`${isArchived ? 'Delete' : 'Archive and delete'} "${node.item.title}"? This cannot be undone.`)) return
    try {
      if (!isArchived) {
        await onArchiveItem(node.item.id)
      }
      await onDeleteItem(node.item.id)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete'
      showToast(message, 'error')
    }
  }, [node.item.id, node.item.title, node.item.isArchived, onArchiveItem, onDeleteItem, showToast])

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleRename()
    if (e.key === 'Escape') {
      setIsEditing(false)
      setEditTitle(node.item.title)
    }
  }, [handleRename, node.item.title])

  const startEditing = useCallback(() => setIsEditing(true), [])
  const cancelEditing = useCallback(() => {
    setIsEditing(false)
    setEditTitle(node.item.title)
  }, [node.item.title])

  return {
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
  }
}
