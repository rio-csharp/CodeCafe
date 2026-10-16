import type { BlockDto } from './types'

export interface BlockNode {
  block: BlockDto
  children: BlockNode[]
}

/**
 * Turns the flat `blocks` list from `PageDetailsDto` into a render tree.
 *
 * Siblings are ordered by `sortKey` using ordinal string comparison — LexoRank
 * ranks are code-unit ordered on purpose, so a locale-aware collator would
 * scramble them.
 *
 * Anything that cannot be reached from a real root (a deleted parent, or a
 * corrupted cycle) is emitted at root level instead of being dropped: the
 * reader shows slightly misplaced content rather than none. Every block is
 * emitted exactly once.
 */
export function assembleBlockTree(blocks: readonly BlockDto[]): BlockNode[] {
  const roots: BlockDto[] = []
  const childrenByParent = new Map<string, BlockDto[]>()

  for (const block of blocks) {
    const parentId = block.parentBlockId

    if (parentId === null || parentId === block.id) {
      roots.push(block)
      continue
    }

    const siblings = childrenByParent.get(parentId)
    if (siblings === undefined) {
      childrenByParent.set(parentId, [block])
    } else {
      siblings.push(block)
    }
  }

  for (const siblings of childrenByParent.values()) {
    siblings.sort(bySortKey)
  }
  roots.sort(bySortKey)

  const emitted = new Set<string>()
  const nodes = roots.map((block) => toNode(block, childrenByParent, emitted))

  const unreachable = blocks.filter((block) => !emitted.has(block.id))
  unreachable.sort(bySortKey)
  for (const block of unreachable) {
    // Reachable after all: it was picked up as somebody's child above.
    if (emitted.has(block.id)) {
      continue
    }
    nodes.push(toNode(block, childrenByParent, emitted))
  }

  return nodes
}

function toNode(
  block: BlockDto,
  childrenByParent: ReadonlyMap<string, BlockDto[]>,
  emitted: Set<string>,
): BlockNode {
  emitted.add(block.id)

  // Already emitted means we looped back onto it — stop rather than recurse.
  const children = (childrenByParent.get(block.id) ?? [])
    .filter((child) => !emitted.has(child.id))
    .map((child) => toNode(child, childrenByParent, emitted))

  return { block, children }
}

/** Ordinal comparison: `<` on strings, never `localeCompare`. */
function bySortKey(a: BlockDto, b: BlockDto): number {
  if (a.sortKey < b.sortKey) {
    return -1
  }
  return a.sortKey > b.sortKey ? 1 : 0
}
