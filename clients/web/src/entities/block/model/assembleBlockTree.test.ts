import { describe, expect, it } from 'vitest'
import type { BlockDto } from './types'
import { assembleBlockTree } from './assembleBlockTree'

function block(id: string, sortKey: string, parentBlockId: string | null = null): BlockDto {
  return {
    id,
    parentBlockId,
    type: 'paragraph',
    content: { spans: [] },
    sortKey,
    version: 1,
    updatedAtUtc: '2026-01-07T12:00:00.000Z',
  }
}

const ids = (nodes: ReturnType<typeof assembleBlockTree>) => nodes.map((node) => node.block.id)

describe('assembleBlockTree', () => {
  it('returns nothing for an empty list', () => {
    expect(assembleBlockTree([])).toEqual([])
  })

  it('orders siblings by sortKey ordinally, so "Z" comes before "a"', () => {
    // A locale collator would put Z last; LexoRank must not be compared that way.
    const tree = assembleBlockTree([block('a', 'a'), block('z', 'Z'), block('b', 'b')])

    expect(ids(tree)).toEqual(['z', 'a', 'b'])
  })

  it('nests children under their parent in the same order', () => {
    const tree = assembleBlockTree([
      block('root', 'a'),
      block('child-b', 'b', 'root'),
      block('child-a', 'a', 'root'),
      block('grandchild', 'a', 'child-a'),
    ])

    expect(ids(tree)).toEqual(['root'])
    expect(tree[0]?.children.map((node) => node.block.id)).toEqual(['child-a', 'child-b'])
    expect(tree[0]?.children[0]?.children.map((node) => node.block.id)).toEqual(['grandchild'])
  })

  it('appends blocks with a missing parent at the root instead of dropping them', () => {
    const tree = assembleBlockTree([
      block('root', 'm'),
      block('orphan-b', 'b', 'deleted-parent'),
      block('orphan-a', 'a', 'deleted-parent'),
    ])

    expect(ids(tree)).toEqual(['root', 'orphan-a', 'orphan-b'])
  })

  it('keeps the children of an orphan with it', () => {
    const tree = assembleBlockTree([
      block('orphan', 'a', 'gone'),
      block('child', 'a', 'orphan'),
    ])

    expect(ids(tree)).toEqual(['orphan'])
    expect(tree[0]?.children.map((node) => node.block.id)).toEqual(['child'])
  })

  it('cuts a corrupted cycle so every block appears exactly once', () => {
    const tree = assembleBlockTree([block('one', 'a', 'two'), block('two', 'b', 'one')])

    expect(ids(tree)).toEqual(['one'])
    expect(tree[0]?.children.map((node) => node.block.id)).toEqual(['two'])
    expect(tree[0]?.children[0]?.children).toEqual([])
  })

  it('treats a self-parented block as a root', () => {
    const tree = assembleBlockTree([block('loop', 'a', 'loop')])

    expect(ids(tree)).toEqual(['loop'])
  })
})
