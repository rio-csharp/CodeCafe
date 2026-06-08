import { useState, useCallback } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import type { TreeNode } from '@/entities/notebook'
import type { Notebook } from '@/entities/notebook'
import { findNode } from '@/entities/notebook'
import { useCreateNotebookItem } from './useCreateNotebookItem'
import { useUpdateNotebookItem } from './useUpdateNotebookItem'
import { useArchiveNotebookItem } from './useArchiveNotebookItem'
import { useRestoreNotebookItem } from './useRestoreNotebookItem'
import { useDeleteNotebookItem } from './useDeleteNotebookItem'
import { useReorderNotebookItems } from './useReorderNotebookItems'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import { computeReorderUpdates } from '../lib/computeReorderUpdates'
import { syncUrlToPathChange } from '../lib/syncUrlToPathChange'

export default function useTreeActions(notebook: Notebook, tree: TreeNode[]) {
  const [draggingId, setDraggingId] = useState<string | null>(null)
  const location = useLocation()
  const navigate = useNavigate()

  // Set of every item id inside the currently-dragged item's subtree
  // (the dragged item itself included). Used by TreeItem.handleDragOver to
  // skip showing an indicator when the cursor is on the dragged item or
  // any of its descendants — those drops are either no-ops or rejected
  // by the server-side descendant check, so the line would be a lie.
  const [draggedSubtreeIds, setDraggedSubtreeIds] = useState<Set<string>>(new Set())

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
      try {
        await createItem.mutateAsync({
          parentId,
          type,
          title,
          sortOrder: 0,
          contentJson: type === 'page' ? { type: 'doc', content: [] } : null,
        })
        showTreeToast('Item created')
      } catch (err) {
        showTreeToast(getErrorMessage(err, 'Failed to create'), 'error')
      }
    },
    [createItem, showTreeToast],
  )

  const handleRenameItem = useCallback(
    async (itemId: string, title: string, sortOrder: number) => {
      // Capture the pre-rename path so we can detect whether the current URL points
      // at the renamed item (or one of its descendants in the folder-rename case)
      // and rewrite it to the new path the server just generated.
      const oldPath = findNode(tree, itemId)?.item.path

      const updated = await updateItem.mutateAsync({ itemId, data: { title, sortOrder } })
      showTreeToast('Renamed')

      if (oldPath) {
        syncUrlToPathChange(oldPath, updated.path, notebook.slug, location.pathname, navigate)
      }
    },
    [updateItem, showTreeToast, tree, location.pathname, navigate, notebook.slug],
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

  const handleDropReorder = useCallback(
    async (targetId: string, position: 'before' | 'after' | 'inside') => {
      if (!draggingId || draggingId === targetId) {
        setDraggingId(null)
        return
      }

      const draggedNode = findNode(tree, draggingId)
      // Backend regenerates the dragged item's path on reorder (and rewrites
      // all descendants' paths when the dragged item is a folder). Capture
      // the pre-mutation path so we can rewrite the URL if the user is
      // currently viewing the dragged item or one of its descendants.
      const oldPath = draggedNode?.item.path

      const result = computeReorderUpdates(tree, draggingId, targetId, position)
      if (!result) {
        setDraggingId(null)
        return
      }

      const { updates } = result

      try {
        const reorderResult = await reorderItems.mutateAsync({ items: updates })
        const updatedDragged = reorderResult.items.find((it) => it.id === draggingId)
        if (oldPath && updatedDragged) {
          syncUrlToPathChange(oldPath, updatedDragged.path, notebook.slug, location.pathname, navigate)
        }
      } catch (err) {
        showTreeToast(getErrorMessage(err, 'Failed to reorder'), 'error')
      } finally {
        setDraggingId(null)
      }
    },
    [draggingId, reorderItems, tree, location.pathname, navigate, notebook.slug, showTreeToast],
  )

  const handleDropOnRoot = useCallback(async () => {
    if (!draggingId) {
      setDraggingId(null)
      return
    }
    if (tree.length > 0) {
      handleDropReorder(tree[0].item.id, 'before')
      return
    }
    // Empty root: the dragged item has to come from a sub-folder, so its
    // path changes from `parent/slug` to just `slug`. Run the same
    // path-aware URL sync the rename/move paths use, otherwise the URL
    // goes stale and a refresh 404s (same class of bug as the rename
    // and drag-reorder URL-sync fixes earlier in this PR).
    const oldPath = findNode(tree, draggingId)?.item.path
    try {
      const result = await reorderItems.mutateAsync({
        items: [{ itemId: draggingId, parentId: null, sortOrder: 0 }],
      })
      const updatedDragged = result.items.find((it) => it.id === draggingId)
      if (oldPath && updatedDragged) {
        syncUrlToPathChange(oldPath, updatedDragged.path, notebook.slug, location.pathname, navigate)
      }
    } catch (err) {
      showTreeToast(getErrorMessage(err, 'Failed to reorder'), 'error')
    } finally {
      setDraggingId(null)
    }
  }, [draggingId, tree, handleDropReorder, reorderItems, location.pathname, navigate, notebook.slug, showTreeToast])

  const handleDragStart = useCallback(
    (id: string) => {
      const node = findNode(tree, id)
      const subtree = new Set<string>()
      if (node) {
        const collect = (n: TreeNode) => {
          subtree.add(n.item.id)
          n.children.forEach(collect)
        }
        collect(node)
      }
      setDraggedSubtreeIds(subtree)
      setDraggingId(id)
    },
    [tree],
  )

  const handleDragEnd = useCallback(() => {
    setDraggedSubtreeIds(new Set())
    setDraggingId(null)
  }, [])

  const dragState = notebook.canEdit
    ? {
        draggingId,
        draggedSubtreeIds,
        onDragStart: handleDragStart,
        onDragEnd: handleDragEnd,
        onDropOnRoot: handleDropOnRoot,
        onDropReorder: handleDropReorder,
      }
    : undefined

  return {
    handleCreateRoot,
    handleCreateItem,
    handleRenameItem,
    handleArchiveItem,
    handleRestoreItem,
    handleDeleteItem,
    dragState,
  }
}
