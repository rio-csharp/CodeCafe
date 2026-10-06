import { describe, expect, it } from 'vitest'
import type { BlockDto } from './types'
import type { BlockNode } from './assembleBlockTree'
import { blockAnchorId, extractOutline } from './outline'

function block(
  id: string,
  type: string,
  content: unknown,
  parentBlockId: string | null = null,
  sortKey = 'a',
): BlockDto {
  return {
    id,
    parentBlockId,
    type,
    content,
    sortKey,
    version: 1,
    updatedAtUtc: '2026-01-07T12:00:00.000Z',
  }
}

const node = (blockValue: BlockDto, children: BlockNode[] = []): BlockNode => ({
  block: blockValue,
  children,
})

const heading = (id: string, level: number, text: string) =>
  block(id, 'heading', { level, spans: [{ text, marks: [] }] })

describe('extractOutline', () => {
  it('collects headings in document order', () => {
    const outline = extractOutline([
      node(heading('one', 2, 'Beans'), [
        node(heading('nested', 3, 'Roast date'), [node(heading('deep', 4, 'Dark'))]),
      ]),
      node(block('p', 'paragraph', { spans: [] })),
      node(heading('two', 2, 'Water')),
    ])

    expect(outline.map((entry) => entry.text)).toEqual(['Beans', 'Roast date', 'Dark', 'Water'])
    expect(outline.map((entry) => entry.level)).toEqual([2, 3, 4, 2])
  })

  it('anchors on the block id, so the link survives a title edit', () => {
    const outline = extractOutline([node(heading('abc', 2, 'Beans'))])

    expect(outline[0]?.id).toBe(blockAnchorId('abc'))
    expect(blockAnchorId('abc')).toBe('block-abc')
  })

  it('skips non-heading blocks and empty headings', () => {
    const outline = extractOutline([
      node(block('code', 'code', { code: 'x', language: 'ts' })),
      node(heading('empty', 2, '   ')),
      node(heading('real', 2, 'Beans')),
    ])

    expect(outline).toHaveLength(1)
    expect(outline[0]?.text).toBe('Beans')
  })

  it('flattens a heading whose text is split across spans', () => {
    const outline = extractOutline([
      node(
        block('split', 'heading', {
          level: 2,
          spans: [{ text: 'Beans', marks: [] }, { text: ' and water', marks: [] }],
        }),
      ),
    ])

    expect(outline[0]?.text).toBe('Beans and water')
  })

  it('clamps an out-of-range level into the tag range', () => {
    const outline = extractOutline([node(heading('deep', 9, 'Beans'))])

    expect(outline[0]?.level).toBe(6)
  })

  it('returns nothing for a page with no headings', () => {
    expect(extractOutline([])).toEqual([])
  })
})
