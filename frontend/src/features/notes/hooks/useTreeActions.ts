import { useState, useCallback } from 'react'
import type { TreeNode } from '../utils/buildTree'
import type { Notebook } from '../types'
import {
  useCreateNotebookItem,
  useUpdateNotebookItem,
  useDeleteNotebookItem,
  useReorderNotebookItems,
} from './useNotesQueries'
import { useToast } from '../../../components/ui/useToast'
import { findSiblings } from '../components/notebook/findSiblings'

export default function useTreeActions(notebook: Notebook, tree: TreeNode[]) {
  const [draggingId, setDraggingId] = useState<string | null>(null)

  const createItem = useCreateNotebookItem(notebook.id)
  const updateItem = useUpdateNotebookItem(notebook.id)
  const deleteItem = useDeleteNotebookItem(notebook.id)
  const reorderItems = useReorderNotebookItems(notebook.id)
  const { showToast: showTreeToast } = useToast()

  const handleCreateRoot = (type: 'folder' | 'page') => {
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
          const msg = err instanceof Error ? err.message : 'Failed to create'
          showTreeToast(msg, 'error')
        },
      },
    )
  }

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

  const handleDeleteItem = useCallback(
    async (itemId: string) => {
      await deleteItem.mutateAsync(itemId)
      showTreeToast('Deleted')
    },
    [deleteItem, showTreeToast],
  )

  const computeReorderPayload = useCallback(
    (siblings: TreeNode[]): { itemId: string; parentId: string | null; sortOrder: number }[] => {
      return siblings.map((node, idx) => ({
        itemId: node.item.id,
        parentId: node.item.parentId,
        sortOrder: idx * 10,
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
      reorderItems.mutate(
        { items: [{ itemId: draggingId, parentId: folderId, sortOrder: 0 }] },
        { onSettled: () => setDraggingId(null) },
      )
    },
    [draggingId, reorderItems],
  )

  const dragState = notebook.canEdit
    ? {
        draggingId,
        onDragStart: setDraggingId,
        onDragEnd: () => setDraggingId(null),
        onDropOnFolder: handleDropOnFolder,
      }
    : undefined

  return {
    handleCreateRoot,
    handleCreateItem,
    handleRenameItem,
    handleDeleteItem,
    handleMoveUp,
    handleMoveDown,
    dragState,
  }
}
