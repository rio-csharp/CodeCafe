import type { BlockDto, BlockOpWire, SpanDto } from '@/entities/block'
import type { EditorBlock } from './draft'
import { spansPlainText } from './spans'

/** Spans-typed payloads with no text are dropped instead of saved empty. */
function isEmptyNewTextBlock(block: EditorBlock): boolean {
  if (!block.isNew || (block.type !== 'paragraph' && block.type !== 'heading')) {
    return false
  }
  const spans = (block.content as { spans?: SpanDto[] }).spans
  return spans === undefined || spansPlainText(spans).trim().length === 0
}

/**
 * Reconciles the edited draft against the page's original blocks. Order:
 * deletes first (so `after` references never point at ghosts), then updates,
 * then inserts in draft order — every insert's `after` names the nearest
 * preceding sibling that survives or was inserted earlier in the batch.
 */
export function diffToOps(
  original: readonly BlockDto[],
  draft: readonly EditorBlock[],
): BlockOpWire[] {
  const survivors = draft.filter((block) => !isEmptyNewTextBlock(block))
  const survivingIds = new Set(
    survivors.filter((block) => !block.isNew).map((block) => block.id),
  )
  const originalById = new Map(original.map((block) => [block.id, block]))

  const ops: BlockOpWire[] = []

  for (const block of original) {
    if (!survivingIds.has(block.id)) {
      ops.push({ kind: 'Delete', blockId: block.id })
    }
  }

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

  const inserted = new Set<string>()
  for (let index = 0; index < survivors.length; index += 1) {
    const block = survivors[index]!
    if (!block.isNew) {
      continue
    }
    let after: string | undefined
    for (let back = index - 1; back >= 0; back -= 1) {
      const previous = survivors[back]!
      if (previous.parentBlockId !== block.parentBlockId) {
        continue
      }
      if (!previous.isNew || inserted.has(previous.id)) {
        after = previous.id
        break
      }
    }
    ops.push({
      kind: 'Insert',
      tempId: block.id,
      type: block.type,
      content: block.content,
      ...(after !== undefined ? { after } : {}),
    })
    inserted.add(block.id)
  }

  return ops
}
