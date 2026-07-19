import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import type { TreeNode } from '@/entities/notebook'
import { useToast } from '@/shared/ui/Toast'
import { useConfirmDialog } from '@/shared/ui/ConfirmDialog'
import { getErrorMessage } from '@/shared/lib/errorUtils'

interface UseTreeNodeActionsOptions {
  node: TreeNode
  onRenameItem: (itemId: string, title: string, sortOrder: number) => Promise<void>
  onArchiveItem: (itemId: string) => Promise<void>
  onRestoreItem: (itemId: string) => Promise<void>
  onDeleteItem: (itemId: string) => Promise<void>
}

export default function useTreeNodeActions({ node, onRenameItem, onArchiveItem, onRestoreItem, onDeleteItem }: UseTreeNodeActionsOptions) {
  const { t } = useTranslation()
  const [isEditing, setIsEditing] = useState(false)
  const [editTitle, setEditTitle] = useState(node.item.title)
  const { showToast } = useToast()
  const { requestConfirm, confirmDialog } = useConfirmDialog()

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
      showToast(getErrorMessage(err, t('notebook.itemRenameFailed')), 'error')
    }
  }, [editTitle, node.item.id, node.item.title, node.item.sortOrder, onRenameItem, showToast, t])

  const handleArchive = useCallback(async () => {
    if (!(await requestConfirm({ title: t('notebook.archiveConfirm', { title: node.item.title }) }))) return
    try {
      await onArchiveItem(node.item.id)
    } catch (err) {
      showToast(getErrorMessage(err, t('notebook.itemArchiveFailed')), 'error')
    }
  }, [node.item.id, node.item.title, onArchiveItem, requestConfirm, showToast, t])

  const handleRestore = useCallback(async () => {
    try {
      await onRestoreItem(node.item.id)
    } catch (err) {
      showToast(getErrorMessage(err, t('notebook.itemRestoreFailed')), 'error')
    }
  }, [node.item.id, onRestoreItem, showToast, t])

  const handleDelete = useCallback(async () => {
    const isArchived = node.item.isArchived
    if (!isArchived) {
      showToast(t('notebook.deleteAfterArchive'), 'error')
      return
    }
    if (!(await requestConfirm({ title: t('notebook.deletePermanentlyConfirm', { title: node.item.title }), danger: true }))) return
    try {
      await onDeleteItem(node.item.id)
    } catch (err) {
      showToast(getErrorMessage(err, t('notebook.itemDeleteFailed')), 'error')
    }
  }, [node.item.id, node.item.title, node.item.isArchived, onDeleteItem, requestConfirm, showToast, t])

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
    confirmDialog,
  }
}
