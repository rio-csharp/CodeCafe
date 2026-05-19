import { describe, it, expect } from 'vitest'
import { extractOutline, slugifyHeadingId } from '../extractOutline'

describe('extractOutline', () => {
  it('returns empty for null', () => {
    expect(extractOutline(null)).toEqual([])
  })

  it('extracts headings from TipTap JSON', () => {
    const doc = {
      type: 'doc',
      content: [
        {
          type: 'heading',
          attrs: { level: 2 },
          content: [{ type: 'text', text: 'Getting Started' }],
        },
        {
          type: 'paragraph',
          content: [{ type: 'text', text: 'Some body text.' }],
        },
        {
          type: 'heading',
          attrs: { level: 3 },
          content: [{ type: 'text', text: 'Installation' }],
        },
      ],
    }
    const outline = extractOutline(doc)
    expect(outline).toEqual([
      { id: 'heading-getting-started', level: 2, text: 'Getting Started' },
      { id: 'heading-installation', level: 3, text: 'Installation' },
    ])
  })

  it('handles nested marks in heading text', () => {
    const doc = {
      type: 'doc',
      content: [
        {
          type: 'heading',
          attrs: { level: 1 },
          content: [
            { type: 'text', text: 'Hello ' },
            { type: 'text', text: 'World', marks: [{ type: 'bold' }] },
          ],
        },
      ],
    }
    const outline = extractOutline(doc)
    expect(outline).toEqual([
      { id: 'heading-hello-world', level: 1, text: 'Hello World' },
    ])
  })

  it('returns empty when no headings', () => {
    const doc = {
      type: 'doc',
      content: [
        {
          type: 'paragraph',
          content: [{ type: 'text', text: 'No headings here.' }],
        },
      ],
    }
    expect(extractOutline(doc)).toEqual([])
  })
})

describe('slugifyHeadingId', () => {
  it('slugifies English text', () => {
    expect(slugifyHeadingId('Getting Started', 0)).toBe('heading-getting-started')
  })

  it('preserves Chinese characters', () => {
    expect(slugifyHeadingId('快速开始指南', 0)).toBe('heading-快速开始指南')
  })

  it('replaces spaces with dashes', () => {
    expect(slugifyHeadingId('Hello World', 0)).toBe('heading-hello-world')
  })

  it('removes special characters', () => {
    // C# & .NET -> c--net-tips (consecutive dashes from removed chars)
    expect(slugifyHeadingId('C# & .NET Tips!', 0)).toBe('heading-c--net-tips')
  })

  it('truncates to 60 chars', () => {
    const long = 'a'.repeat(100)
    expect(slugifyHeadingId(long, 0)).toHaveLength(68) // 'heading-' + 60
  })

  it('falls back to index when text is empty after cleaning', () => {
    expect(slugifyHeadingId('!!!', 5)).toBe('heading-5')
  })

  it('handles mixed Chinese and English', () => {
    expect(slugifyHeadingId('第1章 Introduction', 0)).toBe('heading-第1章-introduction')
  })
})
