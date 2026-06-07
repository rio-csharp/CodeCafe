import { describe, expect, it } from 'vitest'
import { createEmptyTipTapDocument, sanitizeTipTapContent } from './sanitizeTipTapContent'

describe('sanitizeTipTapContent', () => {
  it('removes empty text nodes that ProseMirror rejects', () => {
    const result = sanitizeTipTapContent({
      type: 'doc',
      content: [
        {
          type: 'paragraph',
          content: [
            { type: 'text', text: '' },
            { type: 'text', text: 'Keep me' },
          ],
        },
      ],
    })

    expect(result).toEqual({
      type: 'doc',
      content: [
        {
          type: 'paragraph',
          content: [
            { type: 'text', text: 'Keep me' },
          ],
        },
      ],
    })
  })

  it('returns a fresh empty document when content is missing or malformed', () => {
    const first = sanitizeTipTapContent(null)
    const second = createEmptyTipTapDocument()

    expect(first).toEqual({ type: 'doc', content: [] })
    expect(second).toEqual({ type: 'doc', content: [] })
    expect(first).not.toBe(second)
  })
})
