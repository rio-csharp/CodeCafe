import type { BlockDto, BlockOpWire, SpanDto } from '@/entities/block'
import type { EditorBlock } from './draft'
import { spansPlainText } from './spans'

/**
 * Payloads with no content are dropped instead of saved empty — but only
 * paragraphs (an empty heading/quote/todo is a deliberate conversion result,
 * while an empty paragraph is usually Enter-then-save junk) and untouched
 * media shells (the server rejects an empty image/audio URL).
 */
function isEmptyNewTextBlock(block: EditorBlock): boolean {
  if (!block.isNew) {
    return false
  }
  if (block.type === 'paragraph') {
    const spans = (block.content as { spans?: SpanDto[] }).spans
    return spans === undefined || spansPlainText(spans).trim().length === 0
  }
  if (block.type === 'image' || block.type === 'audio') {
    const url = (block.content as { url?: string }).url
    return url === undefined || url.trim().length === 0
  }
  return false
}

/**
 * Reconciles the edited draft against the page's original blocks.
 *
 * Op order matters, because every op sees the working state left by the
 * earlier ones:
 * 1. Updates — keyed on ORIGINAL versions, before any move bumps them.
 * 2. Moves whose `after`/`parent` reference only surviving original blocks.
 * 3. Inserts — their `after` references already-moved survivors or temp ids.
 * 4. Moves that reference freshly inserted temp ids.
 * 5. Deletes — last, so children re-parented away from a doomed block have
 *    already been moved out of its subtree (deletes cascade).
 * References never name a doomed block: every position is computed among
 * survivors, which is also why deletes no longer need to go first.
 */
export function diffToOps(
  original: readonly BlockDto[],
  draft: readonly EditorBlock[],
): BlockOpWire[] {
  // Empty new paragraphs are junk — but one that survived as somebody's
  // parent must stay, or the children's `parent` reference would not resolve.
  const droppable = new Set(
    draft.filter((block) => isEmptyNewTextBlock(block)).map((block) => block.id),
  )
  let settled = false
  while (!settled) {
    settled = true
    for (const block of draft) {
      if (
        block.parentBlockId !== null &&
        droppable.has(block.parentBlockId) &&
        !droppable.has(block.id)
      ) {
        droppable.delete(block.parentBlockId)
        settled = false
      }
    }
  }
  const survivors = draft.filter((block) => !droppable.has(block.id))

  const originalById = new Map(original.map((block) => [block.id, block]))
  const doomedIds = new Set(
    original.filter((block) => !survivors.some((b) => b.id === block.id)).map((block) => block.id),
  )

  const ops: BlockOpWire[] = []

  for (const block of survivors) {
    if (block.isNew) {
      continue
    }
    const source = originalById.get(block.id)
    if (source === undefined) {
      continue
    }
    if (
      source.type !== block.type ||
      JSON.stringify(source.content) !== JSON.stringify(block.content)
    ) {
      ops.push({
        kind: 'Update',
        blockId: block.id,
        content: block.content,
        baseVersion: source.version,
      })
    }
  }

  // Placement. `sim` mirrors the working state: groups of ids per parent,
  // starting from the original tree minus the doomed, then walked towards the
  // draft's desired shape one block at a time, in draft order. `placed`
  // remembers each group's last-processed block, so every block's desired
  // predecessor is already final when the block is considered.
  const sim = new Map<string | null, string[]>()
  const simParent = new Map<string, string | null>()
  for (const block of original) {
    if (doomedIds.has(block.id)) {
      continue
    }
    const list = sim.get(block.parentBlockId) ?? []
    list.push(block.id)
    sim.set(block.parentBlockId, list)
    simParent.set(block.id, block.parentBlockId)
  }

  const movesEarly: BlockOpWire[] = []
  const movesLate: BlockOpWire[] = []
  const inserts: BlockOpWire[] = []
  const tempIds = new Set(survivors.filter((block) => block.isNew).map((block) => block.id))
  const placed = new Map<string | null, string>()

  for (const block of survivors) {
    const desiredAfter = placed.get(block.parentBlockId)
    placed.set(block.parentBlockId, block.id)

    if (block.isNew) {
      inserts.push({
        kind: 'Insert',
        tempId: block.id,
        type: block.type,
        content: block.content,
        ...(block.parentBlockId !== null ? { parent: block.parentBlockId } : {}),
        ...(desiredAfter !== undefined ? { after: desiredAfter } : {}),
      })
      const group = sim.get(block.parentBlockId) ?? []
      group.push(block.id)
      sim.set(block.parentBlockId, group)
      simParent.set(block.id, block.parentBlockId)
      continue
    }

    const currentParent = simParent.get(block.id) ?? null
    const currentGroup = sim.get(currentParent) ?? []
    const currentIndex = currentGroup.indexOf(block.id)
    const currentAfter = currentIndex > 0 ? currentGroup[currentIndex - 1] : undefined
    if (currentParent === block.parentBlockId && currentAfter === desiredAfter) {
      continue
    }

    const move: BlockOpWire = {
      kind: 'Move',
      blockId: block.id,
      ...(block.parentBlockId !== null ? { parent: block.parentBlockId } : {}),
      ...(desiredAfter !== undefined ? { after: desiredAfter } : {}),
    }
    const referencesTemp =
      (block.parentBlockId !== null && tempIds.has(block.parentBlockId)) ||
      (desiredAfter !== undefined && tempIds.has(desiredAfter))
    if (referencesTemp) {
      movesLate.push(move)
    } else {
      movesEarly.push(move)
    }

    if (currentIndex >= 0) {
      currentGroup.splice(currentIndex, 1)
    }
    const group = sim.get(block.parentBlockId) ?? []
    const targetIndex = desiredAfter === undefined ? 0 : group.indexOf(desiredAfter) + 1
    group.splice(targetIndex, 0, block.id)
    sim.set(block.parentBlockId, group)
    simParent.set(block.id, block.parentBlockId)
  }

  ops.push(...movesEarly, ...inserts, ...movesLate)

  for (const block of original) {
    if (doomedIds.has(block.id)) {
      ops.push({ kind: 'Delete', blockId: block.id })
    }
  }

  return ops
}
