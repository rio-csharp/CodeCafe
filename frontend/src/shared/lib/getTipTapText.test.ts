import { describe, expect, it } from 'vitest'
import { getTipTapText } from './getTipTapText'

describe('getTipTapText', () => {
  it('returns empty string for null or undefined content', () => {
    expect(getTipTapText(null)).toBe('')
    expect(getTipTapText(undefined)).toBe('')
  })

  it('extracts text separated by newlines for block nodes', () => {
    const content = {
      type: 'doc',
      content: [
        { type: 'paragraph', content: [{ type: 'text', text: 'First paragraph' }] },
        { type: 'paragraph', content: [{ type: 'text', text: 'Second paragraph' }] },
      ],
    }

    const text = getTipTapText(content)

    expect(text).toContain('First paragraph')
    expect(text).toContain('Second paragraph')
    expect(text).toMatch(/First paragraph\nSecond paragraph/)
  })

  it('returns empty string for an empty document', () => {
    const text = getTipTapText({ type: 'doc', content: [] })
    expect(text).toBe('')
  })
})
