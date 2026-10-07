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

function fresh(id: string, text: string): EditorBlock {
  return {
    id,
    isNew: true,
    parentBlockId: null,
    type: 'paragraph',
    content: { spans: [{ text, marks: [] }] },
    sortKey: '',
    version: 0,
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
})
