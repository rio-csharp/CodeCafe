import { useState, useCallback } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import type { TreeNode } from '@/entities/notebook'
import type { Notebook } from '@/entities/notebook'
import { findNodeAndSiblings } from '@/entities/notebook'
import { useCreateNotebookItem } from './useCreateNotebookItem'
import { useUpdateNotebookItem } from './useUpdateNotebookItem'
import { useArchiveNotebookItem } from './useArchiveNotebookItem'
import { useRestoreNotebookItem } from './useRestoreNotebookItem'
import { useDeleteNotebookItem } from './useDeleteNotebookItem'
import { useReorderNotebookItems } from './useReorderNotebookItems'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'

function findNode(nodes: TreeNode[], id: string): TreeNode | null {
  for (const node of nodes) {
    if (node.item.id === id) return node
    const found = findNode(node.children, id)
    if (found) return found
  }
  return null
}

function isDescendantOf(nodes: TreeNode[], ancestorId: string): boolean {
  for (const node of nodes) {
    if (node.item.id === ancestorId) return true
    if (isDescendantOf(node.children, ancestorId)) return true
  }
  return false
}

function cloneSiblings(siblings: TreeNode[]): TreeNode[] {
  return siblings.map((n) => ({ item: n.item, children: n.children }))
}

/**
 * If the URL currently points at `oldPath` (or a descendant of it, for the
 * folder-rename/move case), rewrite it to the corresponding tail under
 * `newPath` and `replace` the history entry. No-op when the path didn't
 * actually change or the URL isn't under the notebook's prefix.
 *
 * The descendant case: e.g. URL tail is `folder/oldName/sub/page` and
 * `oldPath` is `folder/oldName`, then `newPath` is `folder/oldName-renamed`
 * and we want the new tail to be `folder/oldName-renamed/sub/page`. The
 * `currentTail.slice(oldPath.length)` swap preserves the descendant suffix.
 */
function syncUrlToPathChange(
  oldPath: string,
  newPath: string,
  notebookSlug: string,
  locationPathname: string,
  navigate: (path: string, opts?: { replace?: boolean }) => void,
): void {
  if (!oldPath || oldPath === newPath) return
  const prefix = `/notes/${notebookSlug}/`
  if (!locationPathname.startsWith(prefix)) return
  const currentTail = locationPathname.slice(prefix.length)
  if (currentTail === oldPath || currentTail.startsWith(`${oldPath}/`)) {
    const newTail = newPath + currentTail.slice(oldPath.length)
    navigate(`${prefix}${newTail}`, { replace: true })
  }
}

export default function useTreeActions(notebook: Notebook, tree: TreeNode[]) {
  const [draggingId, setDraggingId] = useState<string | null>(null)
  const location = useLocation()
  const navigate = useNavigate()

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
      const targetNode = findNode(tree, targetId)
      if (!draggedNode || !targetNode) {
        setDraggingId(null)
        return
      }

      if (position === 'inside') {
        if (targetNode.item.type !== 'folder') {
          setDraggingId(null)
          return
        }
        if (isDescendantOf(draggedNode.children, targetId)) {
          setDraggingId(null)
          return
        }
      }

      const draggedLoc = findNodeAndSiblings(tree, draggingId)
      const targetLoc = findNodeAndSiblings(tree, targetId)
      if (!draggedLoc || !targetLoc) {
        setDraggingId(null)
        return
      }

      // Backend regenerates the dragged item's path on reorder (and rewrites
      // all descendants' paths when the dragged item is a folder). Capture
      // the pre-mutation path so we can rewrite the URL if the user is
      // currently viewing the dragged item or one of its descendants.
      const oldPath = draggedNode.item.path

      // Clone sibling arrays so we never mutate the original tree prop
      const oldSiblings = cloneSiblings(draggedLoc.siblings)
      let newSiblings: TreeNode[]
      let newIndex: number
      let newParentId: string | null

      if (position === 'inside') {
        newSiblings = cloneSiblings(targetNode.children)
        newIndex = newSiblings.length
        newParentId = targetId
      } else {
        newSiblings = cloneSiblings(targetLoc.siblings)
        newIndex = targetLoc.index
        if (draggedLoc.siblings === targetLoc.siblings && draggedLoc.index < targetLoc.index) {
          newIndex--
        }
        if (position === 'after') {
          newIndex++
        }
        newParentId = targetNode.item.parentId
      }

      if (newParentId && (newParentId === draggingId || isDescendantOf(draggedNode.children, newParentId))) {
        setDraggingId(null)
        return
      }

      // Remove from old cloned list and insert into new cloned list
      const draggedIndexInOld = oldSiblings.findIndex((n) => n.item.id === draggingId)
      if (draggedIndexInOld >= 0) {
        oldSiblings.splice(draggedIndexInOld, 1)
      }
      newSiblings.splice(newIndex, 0, draggedNode)

      const updates: { itemId: string; parentId: string | null; sortOrder: number }[] = []

      const recompute = (siblings: TreeNode[]) => {
        siblings.forEach((node, idx) => {
          updates.push({
            itemId: node.item.id,
            parentId: node.item.parentId,
            sortOrder: idx * 10,
          })
        })
      }

      recompute(oldSiblings)
      if (newSiblings !== oldSiblings) {
        recompute(newSiblings)
      }

      // Update dragged item's parentId
      const draggedUpdate = updates.find((u) => u.itemId === draggingId)
      if (draggedUpdate) {
        draggedUpdate.parentId = newParentId
      }

      try {
        const result = await reorderItems.mutateAsync({ items: updates })
        const updatedDragged = result.items.find((it) => it.id === draggingId)
        if (oldPath && updatedDragged) {
          syncUrlToPathChange(oldPath, updatedDragged.path, notebook.slug, location.pathname, navigate)
        }
      } finally {
        setDraggingId(null)
      }
    },
    [draggingId, reorderItems, tree, location.pathname, navigate, notebook.slug],
  )

  const handleDropOnFolder = useCallback(
    (folderId: string) => {
      if (!draggingId || draggingId === folderId) {
        setDraggingId(null)
        return
      }
      handleDropReorder(folderId, 'inside')
    },
    [draggingId, handleDropReorder],
  )

  const handleDropOnRoot = useCallback(() => {
    if (!draggingId) {
      setDraggingId(null)
      return
    }
    if (tree.length > 0) {
      handleDropReorder(tree[0].item.id, 'before')
    } else {
      reorderItems.mutate(
        { items: [{ itemId: draggingId, parentId: null, sortOrder: 0 }] },
        { onSettled: () => setDraggingId(null) },
      )
    }
  }, [draggingId, tree, handleDropReorder, reorderItems])

  const dragState = notebook.canEdit
    ? {
        draggingId,
        onDragStart: setDraggingId,
        onDragEnd: () => setDraggingId(null),
        onDropOnFolder: handleDropOnFolder,
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
