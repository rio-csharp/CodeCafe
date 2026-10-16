import type { PageTreeNode } from '@/entities/notebook'
import type { MovePageData } from '@/entities/page'
import { normalizePagePath } from './tree'

export type DropPosition = 'before' | 'inside' | 'after'

/** Vertical pointer position within a row: the edges reorder, the middle nests. */
export function dropPositionAt(ratio: number): DropPosition {
  if (ratio < 0.3) {
    return 'before'
  }
  if (ratio > 0.7) {
    return 'after'
  }
  return 'inside'
}

/** True when `candidate` is `ancestor` itself or sits anywhere under it. */
export function isSameOrDescendant(ancestorPath: string, candidatePath: string): boolean {
  const ancestor = normalizePagePath(ancestorPath)
  const candidate = normalizePagePath(candidatePath)
  return candidate === ancestor || candidate.startsWith(`${ancestor}/`)
}

interface NodeContext {
  node: PageTreeNode
  /** The parent's path; null for roots. */
  parentPath: string | null
  siblings: readonly PageTreeNode[]
}

function findContext(
  nodes: readonly PageTreeNode[],
  id: string,
  parentPath: string | null,
): NodeContext | null {
  for (const node of nodes) {
    if (node.id === id) {
      return { node, parentPath, siblings: nodes }
    }
    const found = findContext(node.children, id, node.path)
    if (found !== null) {
      return found
    }
  }
  return null
}

/**
 * Turns a drop gesture into a move payload. Null means "not a legal drop":
 * the target is the dragged page or lives in its subtree, or the gesture
 * would leave the page exactly where it already sits.
 */
export function resolveDrop(
  roots: readonly PageTreeNode[],
  draggedId: string,
  targetId: string,
  position: DropPosition,
): MovePageData | null {
  if (draggedId === targetId) {
    return null
  }
  const dragged = findContext(roots, draggedId, null)
  const target = findContext(roots, targetId, null)
  if (dragged === null || target === null) {
    return null
  }

  // A page can never move into its own subtree.
  if (isSameOrDescendant(dragged.node.path, target.node.path)) {
    return null
  }

  if (position === 'inside') {
    const lastChild = target.node.children[target.node.children.length - 1]
    // Already the last child there — nothing would change.
    if (lastChild?.id === draggedId) {
      return null
    }
    return { parentPath: target.node.path, afterPageId: lastChild?.id ?? null }
  }

  const index = target.siblings.findIndex((sibling) => sibling.id === targetId)
  if (position === 'before') {
    const previous = index > 0 ? target.siblings[index - 1] : undefined
    // Already sitting right before the target.
    if (previous?.id === draggedId) {
      return null
    }
    return { parentPath: target.parentPath, afterPageId: previous?.id ?? null }
  }

  const next = target.siblings[index + 1]
  // Already sitting right after the target.
  if (next?.id === draggedId) {
    return null
  }
  return { parentPath: target.parentPath, afterPageId: targetId }
}
