import { useState, useCallback } from 'react'
import type { TreeNode } from '@/entities/notebook'
import type { Notebook } from '@/entities/notebook'
import { useCreateNotebookItem } from './useCreateNotebookItem'
import { useUpdateNotebookItem } from './useUpdateNotebookItem'
import { useArchiveNotebookItem } from './useArchiveNotebookItem'
import { useRestoreNotebookItem } from './useRestoreNotebookItem'
import { useDeleteNotebookItem } from './useDeleteNotebookItem'
import { useReorderNotebookItems } from './useReorderNotebookItems'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { findSiblings } from '@/entities/notebook'

function findNode(nodes: TreeNode[], id: string): TreeNode | null {
  for (const node of nodes) {
    if (node.item.id === id) return node
    const found = findNode(node.children, id)
    if (found) return found
  }
  return null
}

export default function useTreeActions(notebook: Notebook, tree: TreeNode[]) {
  const [draggingId, setDraggingId] = useState<string | null>(null)

  const createItem = useCreateNotebookItem(notebook.id)
  const updateItem = useUpdateNotebookItem(notebook.id)
  const archiveItem = useArchiveNotebookItem(notebook.id)
  const restoreItem = useRestoreNotebookItem(notebook.id)
  const deleteItem = useDeleteNotebookItem(notebook.id)
  const reorderItems = useReorderNotebookItems(notebook.id)
  const { showToast: showTreeToast } = useToast()

  const handleCreateRoot = useCallback((type: 'folder' | 'page') => {
    const title = type === 'folder' ? 'New Folder' : 'New Page'
    createItem.mutate(
      {
        parentId: null,
        type,
        title,
        sortOrder: 0,
        contentJson: type === 'page' ? { type: 'doc', content: [] } : null,
      },
      {
        onSuccess: () => showTreeToast('Item created'),
        onError: (err: unknown) => {
          showTreeToast(getErrorMessage(err, 'Failed to create'), 'error')
        },
      },
    )
  }, [createItem, showTreeToast])

  const handleCreateItem = useCallback(
    async (parentId: string | null, type: 'folder' | 'page') => {
      const title = type === 'folder' ? 'New Folder' : 'New Page'
      await createItem.mutateAsync({
        parentId,
        type,
        title,
        sortOrder: 0,
        contentJson: type === 'page' ? { type: 'doc', content: [] } : null,
      })
      showTreeToast('Item created')
    },
    [createItem, showTreeToast],
  )

  const handleRenameItem = useCallback(
    async (itemId: string, title: string, sortOrder: number) => {
      await updateItem.mutateAsync({ itemId, data: { title, sortOrder } })
      showTreeToast('Renamed')
    },
    [updateItem, showTreeToast],
  )

  const handleArchiveItem = useCallback(
    async (itemId: string) => {
      await archiveItem.mutateAsync(itemId)
      showTreeToast('Archived')
    },
    [archiveItem, showTreeToast],
  )

  const handleRestoreItem = useCallback(
    async (itemId: string) => {
      await restoreItem.mutateAsync(itemId)
      showTreeToast('Restored')
    },
    [restoreItem, showTreeToast],
  )

  const handleDeleteItem = useCallback(
    async (itemId: string) => {
      await deleteItem.mutateAsync(itemId)
      showTreeToast('Deleted')
    },
    [deleteItem, showTreeToast],
  )

  const computeReorderPayload = useCallback(
    (siblings: TreeNode[]): { itemId: string; parentId: string | null; sortOrder: number }[] => {
      const REORDER_STEP = 10
      return siblings.map((node, idx) => ({
        itemId: node.item.id,
        parentId: node.item.parentId,
        sortOrder: idx * REORDER_STEP,
      }))
    },
    [],
  )

  const handleMoveUp = useCallback(
    (itemId: string) => {
      const { siblings, index } = findSiblings(tree, itemId)
      if (index <= 0 || siblings.length < 2) return
      const newSiblings = [...siblings]
      const temp = newSiblings[index - 1]
      newSiblings[index - 1] = newSiblings[index]
      newSiblings[index] = temp
      reorderItems.mutate({ items: computeReorderPayload(newSiblings) })
    },
    [tree, reorderItems, computeReorderPayload],
  )

  const handleMoveDown = useCallback(
    (itemId: string) => {
      const { siblings, index } = findSiblings(tree, itemId)
      if (index < 0 || index >= siblings.length - 1 || siblings.length < 2) return
      const newSiblings = [...siblings]
      const temp = newSiblings[index + 1]
      newSiblings[index + 1] = newSiblings[index]
      newSiblings[index] = temp
      reorderItems.mutate({ items: computeReorderPayload(newSiblings) })
    },
    [tree, reorderItems, computeReorderPayload],
  )

  const handleDropOnFolder = useCallback(
    (folderId: string) => {
      if (!draggingId || draggingId === folderId) {
        setDraggingId(null)
        return
      }
      const targetNode = findNode(tree, folderId)
      const children = targetNode?.children ?? []
      const minSortOrder = children.length > 0
        ? Math.min(...children.map((n) => n.item.sortOrder))
        : 0
      reorderItems.mutate(
        { items: [{ itemId: draggingId, parentId: folderId, sortOrder: minSortOrder - 10 }] },
        { onSettled: () => setDraggingId(null) },
      )
    },
    [draggingId, reorderItems, tree],
  )

  const handleDropOnRoot = useCallback(() => {
    if (!draggingId) {
      setDraggingId(null)
      return
    }
    const minSortOrder = tree.length > 0
      ? Math.min(...tree.map((n) => n.item.sortOrder))
      : 0
    reorderItems.mutate(
      { items: [{ itemId: draggingId, parentId: null, sortOrder: minSortOrder - 10 }] },
      { onSettled: () => setDraggingId(null) },
    )
  }, [draggingId, tree, reorderItems])

  const dragState = notebook.canEdit
    ? {
        draggingId,
        onDragStart: setDraggingId,
        onDragEnd: () => setDraggingId(null),
        onDropOnFolder: handleDropOnFolder,
        onDropOnRoot: handleDropOnRoot,
      }
    : undefined

  return {
    handleCreateRoot,
    handleCreateItem,
    handleRenameItem,
    handleArchiveItem,
    handleRestoreItem,
    handleDeleteItem,
    handleMoveUp,
    handleMoveDown,
    dragState,
  }
}
