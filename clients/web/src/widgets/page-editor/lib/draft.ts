import type { BlockDto } from '@/entities/block'

/**
 * One block inside an editing session. Blocks created mid-session get a
 * client-minted temp id; the batch API maps those to real ids on save.
 */
export interface EditorBlock {
  id: string
  parentBlockId: string | null
  type: string
  content: unknown
  /** LexoRank ordinal — compare with `<`, never with locale collation. */
  sortKey: string
  /** Optimistic-concurrency token for Update ops. */
  version: number
}

/**
 * The draft starts as a faithful, deeply detached copy of the page's blocks:
 * editing mutates payloads in place, so sharing references with the query
 * cache would leak unsaved edits into the reader.
 */
export function toEditorDraft(blocks: readonly BlockDto[]): EditorBlock[] {
  return blocks.map((block) => ({
    id: block.id,
    parentBlockId: block.parentBlockId,
    type: block.type,
    content: structuredClone(block.content),
    sortKey: block.sortKey,
    version: block.version,
  }))
}
