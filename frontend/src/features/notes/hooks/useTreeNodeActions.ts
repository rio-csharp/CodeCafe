import { useState, useCallback } from 'react'
import type { TreeNode } from '../utils/buildTree'
import { useToast } from '../../../components/ui/useToast'

interface UseTreeNodeActionsOptions {
  node: TreeNode
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
}

export default function useTreeNodeActions({ node, onRenameItem, onDeleteItem }: UseTreeNodeActionsOptions) {
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

  const handleDelete = useCallback(async () => {
    if (!confirm(`Delete "${node.item.title}"? This cannot be undone.`)) return
    try {
      await onDeleteItem(node.item.id)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete'
      showToast(message, 'error')
    }
  }, [node.item.id, node.item.title, onDeleteItem, showToast])

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
    handleDelete,
    handleKeyDown,
    startEditing,
    cancelEditing,
  }
}
