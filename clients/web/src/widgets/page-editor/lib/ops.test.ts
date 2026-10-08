import { describe, expect, it } from 'vitest'
import type { BlockDto } from '@/entities/block'
import type { EditorBlock } from './draft'
import { diffToOps } from './ops'

function source(id: string, text: string, version = 1): BlockDto {
  return {
    id,
    parentBlockId: null,
    type: 'paragraph',
    content: { spans: [{ text, marks: [] }] },
    sortKey: id,
    version,
    updatedAtUtc: '2026-01-07T12:00:00.000Z',
  }
}

function edit(block: BlockDto, text: string): EditorBlock {
  return {
    id: block.id,
    isNew: false,
    parentBlockId: block.parentBlockId,
    type: block.type,
    content: { spans: [{ text, marks: [] }] },
    sortKey: block.sortKey,
    version: block.version,
  }
}

function fresh(id: string, text: string, parentBlockId: string | null = null): EditorBlock {
  return {
    id,
    isNew: true,
    parentBlockId,
    type: 'paragraph',
    content: { spans: [{ text, marks: [] }] },
    sortKey: '',
    version: 0,
  }
}

/** An untouched existing block, placed at a (possibly new) parent. */
function placed(block: BlockDto, parentBlockId: string | null): EditorBlock {
  return {
    id: block.id,
    isNew: false,
    parentBlockId,
    type: block.type,
    content: block.content,
    sortKey: block.sortKey,
    version: block.version,
  }
}

describe('diffToOps', () => {
  it('emits nothing when the draft matches the page', () => {
    const original = [source('a', 'one'), source('b', 'two')]

    expect(diffToOps(original, [edit(original[0]!, 'one'), edit(original[1]!, 'two')])).toEqual([])
  })

  it('updates changed blocks with their base version', () => {
    const original = [source('a', 'one', 7)]

    expect(diffToOps(original, [edit(original[0]!, 'ONE')])).toEqual([
      {
        kind: 'Update',
        blockId: 'a',
        content: { spans: [{ text: 'ONE', marks: [] }] },
        baseVersion: 7,
      },
    ])
  })

  it('deletes blocks missing from the draft', () => {
    const original = [source('a', 'one'), source('b', 'two')]

    expect(diffToOps(original, [edit(original[0]!, 'one')])).toEqual([
      { kind: 'Delete', blockId: 'b' },
    ])
  })

  it('anchors each insert at the nearest preceding sibling', () => {
    const original = [source('a', 'one')]
    const draft = [edit(original[0]!, 'one'), fresh('temp-1', 'two'), fresh('temp-2', 'three')]

    expect(diffToOps(original, draft)).toEqual([
      {
        kind: 'Insert',
        tempId: 'temp-1',
        type: 'paragraph',
        content: { spans: [{ text: 'two', marks: [] }] },
        after: 'a',
      },
      {
        kind: 'Insert',
        tempId: 'temp-2',
        type: 'paragraph',
        content: { spans: [{ text: 'three', marks: [] }] },
        after: 'temp-1',
      },
    ])
  })

  it('drops untouched empty paragraphs instead of saving them', () => {
    const original = [source('a', 'one')]
    const draft = [edit(original[0]!, 'one'), fresh('temp-1', '')]

    expect(diffToOps(original, draft)).toEqual([])
  })

  it('splits round-trip: update the left half, insert the right', () => {
    const original = [source('a', 'onetwo')]
    const draft = [edit(original[0]!, 'one'), fresh('temp-1', 'two')]

    const ops = diffToOps(original, draft)
    expect(ops).toHaveLength(2)
    expect(ops[0]).toMatchObject({ kind: 'Update', blockId: 'a' })
    expect(ops[1]).toMatchObject({ kind: 'Insert', tempId: 'temp-1', after: 'a' })
  })

  it('moves a reordered block to its new predecessor', () => {
    const original = [source('a', 'one'), source('b', 'two'), source('c', 'three')]
    const draft = [edit(original[2]!, 'three'), edit(original[0]!, 'one'), edit(original[1]!, 'two')]

    expect(diffToOps(original, draft)).toEqual([{ kind: 'Move', blockId: 'c' }])
  })

  it('indents with a parent reference, the only way into a childless block', () => {
    const original = [source('p', 'parent'), source('b', 'child')]
    const draft = [edit(original[0]!, 'parent'), placed(original[1]!, 'p')]

    expect(diffToOps(original, draft)).toEqual([{ kind: 'Move', blockId: 'b', parent: 'p' }])
  })

  it('outdents by naming the new same-level predecessor', () => {
    const parent = source('p', 'parent')
    const nested = { ...source('c', 'child'), parentBlockId: 'p' }
    const draft = [edit(parent, 'parent'), placed(nested, null)]

    expect(diffToOps([parent, nested], draft)).toEqual([
      { kind: 'Move', blockId: 'c', after: 'p' },
    ])
  })

  it('moves rescued children out of a doomed subtree BEFORE deleting it', () => {
    const parent = source('p', 'parent')
    const doomed = source('b', 'gone')
    const orphan = { ...source('c', 'orphan'), parentBlockId: 'b' }
    // b is merged away; its child c is re-parented onto p.
    const draft = [edit(parent, 'parentgone'), placed(orphan, 'p')]

    const ops = diffToOps([parent, doomed, orphan], draft)
    expect(ops.map((op) => op.kind)).toEqual(['Update', 'Move', 'Delete'])
    expect(ops[1]).toMatchObject({ kind: 'Move', blockId: 'c', parent: 'p' })
    expect(ops[2]).toMatchObject({ kind: 'Delete', blockId: 'b' })
  })

  it('defers moves that reference a fresh temp id until after the inserts', () => {
    const old = source('x', 'old')
    const nested = { ...source('c', 'child'), parentBlockId: 'x' }
    // Slash-conversion: x leaves, a fresh block takes its place and its child.
    const draft = [fresh('temp-1', 'new'), placed(nested, 'temp-1')]

    const ops = diffToOps([old, nested], draft)
    expect(ops.map((op) => op.kind)).toEqual(['Insert', 'Move', 'Delete'])
    expect(ops[1]).toMatchObject({ kind: 'Move', blockId: 'c', parent: 'temp-1' })
    expect(ops[2]).toMatchObject({ kind: 'Delete', blockId: 'x' })
  })

  it('keeps an empty new paragraph that survived as a parent', () => {
    const original = [source('b', 'child')]
    const draft = [fresh('temp-1', ''), placed(original[0]!, 'temp-1')]

    const ops = diffToOps(original, draft)
    expect(ops.map((op) => op.kind)).toEqual(['Insert', 'Move'])
    expect(ops[0]).toMatchObject({ kind: 'Insert', tempId: 'temp-1' })
    expect(ops[1]).toMatchObject({ kind: 'Move', blockId: 'b', parent: 'temp-1' })
  })
})
