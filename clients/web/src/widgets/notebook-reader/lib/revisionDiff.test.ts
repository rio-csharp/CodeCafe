import { describe, expect, it } from 'vitest'
import type { BlockDto } from '@/entities/block'
import { blockPlainText, diffPageAtRevision } from './revisionDiff'

function block(id: string, text: string, sortKey = 'a', parentBlockId: string | null = null): BlockDto {
  return {
    id,
    parentBlockId,
    type: 'paragraph',
    content: { spans: [{ text, marks: [] }] },
    sortKey,
    version: 1,
    updatedAtUtc: '2026-01-01T00:00:00.000Z',
  }
}

describe('diffPageAtRevision', () => {
  it('reports additions in current order and removals in past order', () => {
    const past = [block('gone-1', 'old one', 'a'), block('gone-2', 'old two', 'b')]
    const current = [block('new-1', 'fresh one', 'a'), block('new-2', 'fresh two', 'b')]

    const entries = diffPageAtRevision(current, past)

    expect(entries.map((entry) => [entry.kind, entry.blockId])).toEqual([
      ['removed', 'gone-1'],
      ['removed', 'gone-2'],
      ['added', 'new-1'],
      ['added', 'new-2'],
    ])
    expect(entries[0]).toMatchObject({ beforeText: 'old one', afterText: null })
    expect(entries[2]).toMatchObject({ beforeText: null, afterText: 'fresh one' })
  })

  it('reports content edits with before and after text', () => {
    const entries = diffPageAtRevision([block('b1', 'now')], [block('b1', 'then')])

    expect(entries).toEqual([
      expect.objectContaining({
        kind: 'changed',
        contentChanged: true,
        placementChanged: false,
        beforeText: 'then',
        afterText: 'now',
      }),
    ])
  })

  it('reports pure moves as placement-only changes', () => {
    const entries = diffPageAtRevision([block('b1', 'same', 'b')], [block('b1', 'same', 'a')])

    expect(entries).toEqual([
      expect.objectContaining({ kind: 'changed', contentChanged: false, placementChanged: true }),
    ])
  })

  it('returns an empty diff for identical states', () => {
    expect(diffPageAtRevision([block('b1', 'same')], [block('b1', 'same')])).toEqual([])
  })
})

describe('blockPlainText', () => {
  it('reads spans and code, and gives up on the rest', () => {
    expect(blockPlainText(block('b1', 'hello'))).toBe('hello')
    expect(
      blockPlainText({ ...block('b2', ''), type: 'code', content: { code: 'let x = 1', language: 'js' } }),
    ).toBe('let x = 1')
    expect(blockPlainText({ ...block('b3', ''), type: 'divider', content: {} })).toBeNull()
  })
})
