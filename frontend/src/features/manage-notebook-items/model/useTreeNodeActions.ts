import { useState, useCallback } from 'react'
import type { TreeNode } from '@/entities/notebook'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'

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
      showToast(getErrorMessage(err, 'Failed to rename'), 'error')
    }
  }, [editTitle, node.item.id, node.item.title, node.item.sortOrder, onRenameItem, showToast])

  const handleArchive = useCallback(async () => {
    if (!confirm(`Archive "${node.item.title}"? It will be hidden from the notebook.`)) return
    try {
      await onArchiveItem(node.item.id)
    } catch (err) {
      showToast(getErrorMessage(err, 'Failed to archive'), 'error')
    }
  }, [node.item.id, node.item.title, onArchiveItem, showToast])

  const handleRestore = useCallback(async () => {
    try {
      await onRestoreItem(node.item.id)
    } catch (err) {
      showToast(getErrorMessage(err, 'Failed to restore'), 'error')
    }
  }, [node.item.id, onRestoreItem, showToast])

  const handleDelete = useCallback(async () => {
    const isArchived = node.item.isArchived
    if (!confirm(`${isArchived ? 'Delete' : 'Archive and delete'} "${node.item.title}"? This cannot be undone.`)) return
    try {
      if (!isArchived) {
        await onArchiveItem(node.item.id)
      }
      await onDeleteItem(node.item.id)
    } catch (err) {
      showToast(getErrorMessage(err, 'Failed to delete'), 'error')
    }
  }, [node.item.id, node.item.title, node.item.isArchived, onArchiveItem, onDeleteItem, showToast])

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Enter') handleRename()
    if (e.key === 'Escape') {
      setIsEditing(false)
      setEditTitle(node.item.title)
    }
  }, [handleRename, node.item.title])

  const startEditing = useCallback(() => {
    setEditTitle(node.item.title)
    setIsEditing(true)
  }, [node.item.title])
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
