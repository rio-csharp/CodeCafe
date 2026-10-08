import { describe, expect, it } from 'vitest'
import type { EditorBlock } from './draft'
import {
  hoistChildren,
  indentBlock,
  moveBlockInGroup,
  outdentBlock,
  transferChildren,
} from './moving'

function block(id: string, parentBlockId: string | null = null): EditorBlock {
  return {
    id,
    isNew: false,
    parentBlockId,
    type: 'paragraph',
    content: { spans: [{ text: id, marks: [] }] },
    sortKey: id,
    version: 1,
  }
}

/** Ids in preorder, with `parent:` marking nesting — the draft's visible shape. */
function shape(draft: readonly EditorBlock[]): string[] {
  return draft.map((entry) =>
    entry.parentBlockId === null ? entry.id : `${entry.parentBlockId}:${entry.id}`,
  )
}

describe('indentBlock', () => {
  it('makes a block the last child of its previous sibling', () => {
    const draft = [block('a'), block('b'), block('c')]

    const next = indentBlock(draft, 'b')

    expect(next).not.toBeNull()
    expect(shape(next!)).toEqual(['a', 'a:b', 'c'])
  })

  it('appends under a sibling that already has children', () => {
    const draft = [block('a'), block('x', 'a'), block('b')]

    const next = indentBlock(draft, 'b')

    expect(shape(next!)).toEqual(['a', 'a:x', 'a:b'])
  })

  it('is a no-op for the first sibling', () => {
    const draft = [block('a'), block('b')]

    expect(indentBlock(draft, 'a')).toBeNull()
  })
})

describe('outdentBlock', () => {
  it('makes a block the next sibling of its parent', () => {
    const draft = [block('a'), block('b', 'a'), block('c')]

    const next = outdentBlock(draft, 'b')

    expect(shape(next!)).toEqual(['a', 'b', 'c'])
  })

  it('is a no-op at the top level', () => {
    expect(outdentBlock([block('a'), block('b')], 'a')).toBeNull()
  })
})

describe('moveBlockInGroup', () => {
  it('swaps with the neighbour in the given direction', () => {
    const draft = [block('a'), block('b'), block('c')]

    expect(shape(moveBlockInGroup(draft, 'b', -1)!)).toEqual(['b', 'a', 'c'])
    expect(shape(moveBlockInGroup(draft, 'b', 1)!)).toEqual(['a', 'c', 'b'])
  })

  it('is a no-op at the group edges', () => {
    const draft = [block('a'), block('b')]

    expect(moveBlockInGroup(draft, 'a', -1)).toBeNull()
    expect(moveBlockInGroup(draft, 'b', 1)).toBeNull()
  })

  it('does not move across levels', () => {
    const draft = [block('a'), block('x', 'a')]

    expect(moveBlockInGroup(draft, 'x', -1)).toBeNull()
  })
})

describe('hoistChildren', () => {
  it('replaces the block with its children in place', () => {
    const draft = [block('a'), block('b'), block('x', 'b'), block('y', 'b'), block('c')]

    expect(shape(hoistChildren(draft, 'b'))).toEqual(['a', 'x', 'y', 'c'])
  })
})

describe('transferChildren', () => {
  it('appends the children to the merge target', () => {
    const draft = [block('a'), block('x', 'a'), block('b'), block('y', 'b')]

    expect(shape(transferChildren(draft, 'b', 'a'))).toEqual(['a', 'a:x', 'a:y', 'b'])
  })
})
