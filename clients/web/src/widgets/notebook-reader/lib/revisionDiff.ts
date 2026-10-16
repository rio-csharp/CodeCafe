import type { BlockDto } from '@/entities/block'

export type RevisionDiffKind = 'added' | 'removed' | 'changed'

export interface RevisionDiffEntry {
  blockId: string
  type: string
  kind: RevisionDiffKind
  /** Payload differs (ordinal JSON compare). */
  contentChanged: boolean
  /** Parent or sort key differs. */
  placementChanged: boolean
  /** Plain-text snippets for the list; null when the block type carries no text. */
  beforeText: string | null
  afterText: string | null
}

/** Best-effort plain text of a block payload; null for non-textual types. */
export function blockPlainText(block: BlockDto): string | null {
  const content = block.content as Record<string, unknown>
  const spans = content?.spans as { text: string }[] | undefined
  if (spans !== undefined) {
    return spans.map((span) => span.text).join('')
  }
  const code = content?.code
  return typeof code === 'string' ? code : null
}

/**
 * Block-level diff between the page's CURRENT state and a historical snapshot.
 * Entries come in reading order of the version that owns them: changed and
 * removed blocks follow the past order, additions the current one.
 */
export function diffPageAtRevision(
  current: readonly BlockDto[],
  past: readonly BlockDto[],
): RevisionDiffEntry[] {
  const currentById = new Map(current.map((block) => [block.id, block]))
  const pastById = new Map(past.map((block) => [block.id, block]))

  const entries: RevisionDiffEntry[] = []
  for (const before of past) {
    const after = currentById.get(before.id)
    if (after === undefined) {
      entries.push({
        blockId: before.id,
        type: before.type,
        kind: 'removed',
        contentChanged: false,
        placementChanged: false,
        beforeText: blockPlainText(before),
        afterText: null,
      })
      continue
    }
    const contentChanged = JSON.stringify(before.content) !== JSON.stringify(after.content)
    const placementChanged =
      before.parentBlockId !== after.parentBlockId || before.sortKey !== after.sortKey
    if (contentChanged || placementChanged) {
      entries.push({
        blockId: before.id,
        type: before.type,
        kind: 'changed',
        contentChanged,
        placementChanged,
        beforeText: blockPlainText(before),
        afterText: blockPlainText(after),
      })
    }
  }
  for (const after of current) {
    if (!pastById.has(after.id)) {
      entries.push({
        blockId: after.id,
        type: after.type,
        kind: 'added',
        contentChanged: false,
        placementChanged: false,
        beforeText: null,
        afterText: blockPlainText(after),
      })
    }
  }
  return entries
}
