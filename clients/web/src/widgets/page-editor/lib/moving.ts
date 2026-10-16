import type { EditorBlock } from './draft'

/**
 * Block-moving operations on the flat draft. Each one rewrites the relevant
 * parent/child references and returns the draft re-flattened in preorder, or
 * null when the gesture is a no-op (first sibling under Tab, top-level block
 * under Shift+Tab, …). The diff turns the new shape into Move ops on save.
 */

/** Groups the flat draft by parent, keeping array order within each group. */
function groupByParent(draft: readonly EditorBlock[]): Map<string | null, EditorBlock[]> {
  const groups = new Map<string | null, EditorBlock[]>()
  for (const block of draft) {
    const list = groups.get(block.parentBlockId) ?? []
    list.push(block)
    groups.set(block.parentBlockId, list)
  }
  return groups
}

/** Preorder flatten; blocks unreachable from the root (a corrupt draft) append at the end. */
function flatten(groups: Map<string | null, EditorBlock[]>): EditorBlock[] {
  const placed = new Set<string>()
  const ordered: EditorBlock[] = []
  const walk = (parentId: string | null): void => {
    for (const block of groups.get(parentId) ?? []) {
      if (placed.has(block.id)) {
        continue
      }
      placed.add(block.id)
      ordered.push(block)
      walk(block.id)
    }
  }
  walk(null)
  for (const list of groups.values()) {
    for (const block of list) {
      if (!placed.has(block.id)) {
        ordered.push(block)
      }
    }
  }
  return ordered
}

/** The draft in visible (preorder) order — the order navigation operates in. */
export function preorderBlocks(draft: readonly EditorBlock[]): EditorBlock[] {
  return flatten(groupByParent(draft))
}

/** Removes blocks together with every descendant, cascading like the server does. */
export function removeBlocks(
  draft: readonly EditorBlock[],
  ids: ReadonlySet<string>,
): EditorBlock[] {
  const doomed = new Set(ids)
  let settled = false
  while (!settled) {
    settled = true
    for (const block of draft) {
      if (
        block.parentBlockId !== null &&
        doomed.has(block.parentBlockId) &&
        !doomed.has(block.id)
      ) {
        doomed.add(block.id)
        settled = false
      }
    }
  }
  return draft.filter((block) => !doomed.has(block.id))
}

/** Tab: become the LAST child of the previous sibling, Notion-style. */
export function indentBlock(draft: readonly EditorBlock[], id: string): EditorBlock[] | null {
  const block = draft.find((entry) => entry.id === id)
  if (block === undefined) {
    return null
  }
  const groups = groupByParent(draft)
  const group = groups.get(block.parentBlockId) ?? []
  const index = group.findIndex((entry) => entry.id === id)
  if (index <= 0) {
    return null
  }
  const newParent = group[index - 1]!
  group.splice(index, 1)
  groups.set(newParent.id, [
    ...(groups.get(newParent.id) ?? []),
    { ...block, parentBlockId: newParent.id },
  ])
  return flatten(groups)
}

/**
 * Shift+Tab: become the parent's next sibling. The parent's later siblings
 * stay with it — they do NOT follow the outdented block.
 */
export function outdentBlock(draft: readonly EditorBlock[], id: string): EditorBlock[] | null {
  const block = draft.find((entry) => entry.id === id)
  if (block === undefined || block.parentBlockId === null) {
    return null
  }
  const parent = draft.find((entry) => entry.id === block.parentBlockId)
  if (parent === undefined) {
    return null
  }
  const groups = groupByParent(draft)
  const childGroup = groups.get(parent.id) ?? []
  const index = childGroup.findIndex((entry) => entry.id === id)
  if (index < 0) {
    return null
  }
  childGroup.splice(index, 1)
  const grandGroup = groups.get(parent.parentBlockId) ?? []
  const parentIndex = grandGroup.findIndex((entry) => entry.id === parent.id)
  grandGroup.splice(parentIndex + 1, 0, { ...block, parentBlockId: parent.parentBlockId })
  return flatten(groups)
}

/** Ctrl/Cmd+Shift+Arrow: swap with the previous/next sibling in the same group. */
export function moveBlockInGroup(
  draft: readonly EditorBlock[],
  id: string,
  direction: -1 | 1,
): EditorBlock[] | null {
  const block = draft.find((entry) => entry.id === id)
  if (block === undefined) {
    return null
  }
  const groups = groupByParent(draft)
  const group = groups.get(block.parentBlockId) ?? []
  const index = group.findIndex((entry) => entry.id === id)
  const swapWith = index + direction
  if (index < 0 || swapWith < 0 || swapWith >= group.length) {
    return null
  }
  ;[group[index], group[swapWith]] = [group[swapWith]!, group[index]!]
  return flatten(groups)
}

/**
 * Replaces a block with its children in place. Used when an empty shell is
 * deleted: the children keep their relative order at the shell's old spot.
 */
export function hoistChildren(draft: readonly EditorBlock[], id: string): EditorBlock[] {
  const block = draft.find((entry) => entry.id === id)
  if (block === undefined) {
    return [...draft]
  }
  const groups = groupByParent(draft)
  const children = (groups.get(id) ?? []).map((child) => ({
    ...child,
    parentBlockId: block.parentBlockId,
  }))
  const group = groups.get(block.parentBlockId) ?? []
  const index = group.findIndex((entry) => entry.id === id)
  if (index >= 0) {
    group.splice(index, 1, ...children)
  }
  groups.delete(id)
  return flatten(groups)
}

/**
 * Moves every child of `fromId` to the END of `toId`'s children — the merge
 * case, where the merged-away block's children follow the surviving text.
 */
export function transferChildren(
  draft: readonly EditorBlock[],
  fromId: string,
  toId: string,
): EditorBlock[] {
  const groups = groupByParent(draft)
  const children = (groups.get(fromId) ?? []).map((child) => ({ ...child, parentBlockId: toId }))
  groups.delete(fromId)
  groups.set(toId, [...(groups.get(toId) ?? []), ...children])
  return flatten(groups)
}

/**
 * Drag-and-drop placement: `dragId` becomes a sibling of `targetId`, directly
 * before or after it. Returns null for a no-op (self-drop, or dropping into
 * the dragged block's own subtree, which would create a cycle).
 */
export function relocateBlock(
  draft: readonly EditorBlock[],
  dragId: string,
  targetId: string,
  position: 'before' | 'after',
): EditorBlock[] | null {
  if (dragId === targetId) {
    return null
  }
  const dragged = draft.find((entry) => entry.id === dragId)
  const target = draft.find((entry) => entry.id === targetId)
  if (dragged === undefined || target === undefined) {
    return null
  }
  let ancestor = target.parentBlockId
  while (ancestor !== null) {
    if (ancestor === dragId) {
      return null
    }
    ancestor = draft.find((entry) => entry.id === ancestor)?.parentBlockId ?? null
  }
  const groups = groupByParent(draft)
  const origin = groups.get(dragged.parentBlockId) ?? []
  groups.set(
    dragged.parentBlockId,
    origin.filter((entry) => entry.id !== dragId),
  )
  const siblings = groups.get(target.parentBlockId) ?? []
  const at = siblings.findIndex((entry) => entry.id === targetId)
  siblings.splice(position === 'before' ? at : at + 1, 0, {
    ...dragged,
    parentBlockId: target.parentBlockId,
  })
  groups.set(target.parentBlockId, siblings)
  return flatten(groups)
}
