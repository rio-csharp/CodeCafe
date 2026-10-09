import { randomId } from '@/shared/lib'
import { assembleBlockTree } from '@/entities/block'
import type { BlockDto, BlockNode } from '@/entities/block'

/**
 * One block inside an editing session. Blocks created mid-session get a
 * client-minted `temp-*` id; the batch API maps those to real ids on save.
 */
export interface EditorBlock {
  id: string
  isNew: boolean
  parentBlockId: string | null
  type: string
  content: unknown
  /** LexoRank ordinal — compare with `<`, never with locale collation. */
  sortKey: string
  /** Optimistic-concurrency token for Update ops. */
  version: number
}

/**
 * The draft starts as a faithful copy of the page's blocks in preorder (the
 * display order), deeply detached: editing mutates payloads in place, so
 * sharing references with the query cache would leak unsaved edits into the
 * reader.
 */
export function toEditorDraft(blocks: readonly BlockDto[]): EditorBlock[] {
  const draft: EditorBlock[] = []
  const walk = (nodes: readonly BlockNode[]): void => {
    for (const node of nodes) {
      draft.push(toEditorBlock(node.block))
      walk(node.children)
    }
  }
  walk(assembleBlockTree(blocks))
  return draft
}

export function mintTempId(): string {
  return `temp-${randomId()}`
}

/** A fresh paragraph, unsaved until the batch lands. */
export function emptyParagraphBlock(): EditorBlock {
  return {
    id: mintTempId(),
    isNew: true,
    parentBlockId: null,
    type: 'paragraph',
    content: { spans: [] },
    sortKey: '',
    version: 0,
  }
}

export function replaceBlockContent(
  draft: readonly EditorBlock[],
  id: string,
  content: unknown,
): EditorBlock[] {
  return draft.map((block) => (block.id === id ? { ...block, content } : block))
}

/** Sibling insertion: the new block lands right after `afterId` in the array. */
export function insertBlockAfter(
  draft: readonly EditorBlock[],
  afterId: string,
  block: EditorBlock,
): EditorBlock[] {
  return insertBlocksAfter(draft, afterId, [block])
}

/** Batch sibling insertion, same landing spot as {@link insertBlockAfter}. */
export function insertBlocksAfter(
  draft: readonly EditorBlock[],
  afterId: string,
  blocks: readonly EditorBlock[],
): EditorBlock[] {
  const index = draft.findIndex((entry) => entry.id === afterId)
  if (index === -1) {
    return [...draft, ...blocks]
  }
  return [...draft.slice(0, index + 1), ...blocks, ...draft.slice(index + 1)]
}

export function removeBlock(draft: readonly EditorBlock[], id: string): EditorBlock[] {
  return draft.filter((block) => block.id !== id)
}

function toEditorBlock(block: BlockDto): EditorBlock {
  return {
    id: block.id,
    isNew: false,
    parentBlockId: block.parentBlockId,
    type: block.type,
    content: structuredClone(block.content),
    sortKey: block.sortKey,
    version: block.version,
  }
}
