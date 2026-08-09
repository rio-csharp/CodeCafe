import type { TreeNode } from '@/entities/notebook'
import { findNode, findNodeAndSiblings } from '@/entities/notebook'

export interface ReorderUpdate {
  itemId: string
  parentId: string | null
  sortOrder: number
}

function cloneSiblings(siblings: TreeNode[]): TreeNode[] {
  return siblings.map((n) => ({ item: n.item, children: n.children }))
}

/**
 * Pure helper that computes the list of item updates required to reorder
 * `draggingId` relative to `targetId` within a tree.
 *
 * Returns `null` when the drop would be invalid (missing nodes, or trying to
 * drop an item onto one of its own descendants).
 */
export function computeReorderUpdates(
  tree: TreeNode[],
  draggingId: string,
  targetId: string,
  position: 'before' | 'after' | 'inside',
): { updates: ReorderUpdate[] } | null {
  const draggedLoc = findNodeAndSiblings(tree, draggingId)
  const targetLoc = findNodeAndSiblings(tree, targetId)
  if (!draggedLoc || !targetLoc) {
    return null
  }
  const draggedNode = draggedLoc.node
  const targetNode = targetLoc.node

  let destination: TreeNode[]
  let newIndex: number
  let newParentId: string | null

  if (position === 'inside') {
    destination = targetNode.children
    newIndex = destination.length
    newParentId = targetId
  } else if (targetNode.item.type === 'folder' && targetNode.children.length === 0) {
    // Empty folder with a 'before'/'after' Y-position intent. The
    // visual is the same thin line as any other item, but the drop
    // means "put me in this folder as the only child" — same parent
    // rewrite as the 'inside' branch above.
    destination = targetNode.children
    newIndex = 0
    newParentId = targetId
  } else {
    destination = targetLoc.siblings
    newIndex = targetLoc.index
    if (destination === draggedLoc.siblings && draggedLoc.index < targetLoc.index) {
      newIndex--
    }
    if (position === 'after') {
      newIndex++
    }
    newParentId = targetNode.item.parentId
  }

  if (
    newParentId &&
    (newParentId === draggingId || findNode(draggedNode.children, newParentId) !== null)
  ) {
    return null
  }

  // Clone the destination list so we never mutate the original tree prop.
  // The dragged item may already be in it — reordering within the same
  // parent, or dropping 'inside' the item's current parent — so strip it
  // first and the splice below can never produce a duplicate entry.
  const newSiblings = cloneSiblings(destination)
  const draggedIndexInNew = newSiblings.findIndex((n) => n.item.id === draggingId)
  if (draggedIndexInNew >= 0) {
    newSiblings.splice(draggedIndexInNew, 1)
  }
  newSiblings.splice(newIndex, 0, draggedNode)

  const updates: ReorderUpdate[] = []

  const recompute = (siblings: TreeNode[]) => {
    siblings.forEach((node, idx) => {
      const existingIndex = updates.findIndex((u) => u.itemId === node.item.id)
      const update = {
        itemId: node.item.id,
        parentId: node.item.parentId,
        sortOrder: idx * 10,
      }
      if (existingIndex >= 0) {
        // Overwrite the duplicate: the dragged item appears in both lists (old
        // and new siblings) when reordering within the same parent, so the later
        // recompute wins.
        updates[existingIndex] = update
      } else {
        updates.push(update)
      }
    })
  }

  // Only when the dragged item actually left its old list does that list
  // need its survivors' sort orders recomputed; for a same-list reorder the
  // single recompute of newSiblings below covers everything.
  if (destination !== draggedLoc.siblings) {
    recompute(cloneSiblings(draggedLoc.siblings).filter((n) => n.item.id !== draggingId))
  }
  recompute(newSiblings)

  // Update dragged item's parentId
  const draggedUpdate = updates.find((u) => u.itemId === draggingId)
  if (draggedUpdate) {
    draggedUpdate.parentId = newParentId
  }

  return { updates }
}
