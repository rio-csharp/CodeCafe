import { describe, expect, it } from 'vitest'
import type { BlockDto } from '@/entities/block'
import { toEditorDraft } from './draft'

function block(overrides: Partial<BlockDto> = {}): BlockDto {
  return {
    id: 'block-1',
    parentBlockId: null,
    type: 'paragraph',
    content: { spans: [{ text: 'Start with fresh beans.', marks: [] }] },
    sortKey: 'a',
    version: 3,
    updatedAtUtc: '2026-01-07T12:00:00.000Z',
    ...overrides,
  }
}

describe('toEditorDraft', () => {
  it('copies every field the save pipeline needs', () => {
    const [entry] = toEditorDraft([block()])

    expect(entry).toEqual({
      id: 'block-1',
      isNew: false,
      parentBlockId: null,
      type: 'paragraph',
      content: { spans: [{ text: 'Start with fresh beans.', marks: [] }] },
      sortKey: 'a',
      version: 3,
    })
  })

  it('detaches payloads so draft edits never leak into the query cache', () => {
    const source = block()
    const [entry] = toEditorDraft([source])

    ;(entry!.content as { spans: { text: string }[] }).spans[0]!.text = 'edited'

    expect((source.content as { spans: { text: string }[] }).spans[0]!.text).toBe(
      'Start with fresh beans.',
    )
  })
})
