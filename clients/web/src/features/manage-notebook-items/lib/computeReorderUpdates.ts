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
): { updates: ReorderUpdate[]; draggedParentId: string | null } | null {
  const draggedLoc = findNodeAndSiblings(tree, draggingId)
  const targetLoc = findNodeAndSiblings(tree, targetId)
  if (!draggedLoc || !targetLoc) {
    return null
  }
  const draggedNode = draggedLoc.node
  const targetNode = targetLoc.node

  const isSameSiblings = draggedLoc.siblings === targetLoc.siblings

  // Clone sibling arrays so we never mutate the original tree prop
  const oldSiblings = cloneSiblings(draggedLoc.siblings)
  let newSiblings: TreeNode[]
  let newIndex: number
  let newParentId: string | null

  if (position === 'inside') {
    newSiblings = cloneSiblings(targetNode.children)
    newIndex = newSiblings.length
    newParentId = targetId
  } else if (targetNode.item.type === 'folder' && targetNode.children.length === 0) {
    // Empty folder with a 'before'/'after' Y-position intent. The
    // visual is the same thin line as any other item, but the drop
    // means "put me in this folder as the only child" — same parent
    // rewrite as the 'inside' branch above.
    newSiblings = cloneSiblings(targetNode.children)
    newIndex = 0
    newParentId = targetId
  } else {
    newSiblings = cloneSiblings(targetLoc.siblings)
    newIndex = targetLoc.index
    if (isSameSiblings && draggedLoc.index < targetLoc.index) {
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

  // Remove dragged from its current slot in the source list.
  const draggedIndexInOld = oldSiblings.findIndex((n) => n.item.id === draggingId)
  if (draggedIndexInOld >= 0) {
    oldSiblings.splice(draggedIndexInOld, 1)
  }

  // If the destination IS the same list (i.e. reordering within the
  // same parent), the cloned newSiblings still contains the dragged
  // item and the splice below would duplicate it. The newIndex above
  // was already adjusted for the removal, so strip the dragged item
  // out of newSiblings first and the splice produces the final order.
  if (isSameSiblings && position !== 'inside') {
    const draggedIndexInNew = newSiblings.findIndex((n) => n.item.id === draggingId)
    if (draggedIndexInNew >= 0) {
      newSiblings.splice(draggedIndexInNew, 1)
    }
  }

  newSiblings.splice(newIndex, 0, draggedNode)

  const updates: ReorderUpdate[] = []

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

  return { updates, draggedParentId: newParentId }
}
