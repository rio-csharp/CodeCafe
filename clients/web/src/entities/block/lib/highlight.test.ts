import { describe, expect, it } from 'vitest'
import { highlightCode } from './highlight'

describe('highlightCode', () => {
  it('wraps keywords in token spans for a known language', () => {
    const html = highlightCode('const x = 1;', 'javascript')

    expect(html).toContain('hljs-keyword')
    expect(html).toContain('const')
  })

  it('returns escaped plain text for an unknown language', () => {
    expect(highlightCode('a < b', 'nonsense-lang')).toBe('a &lt; b')
    expect(highlightCode('a < b', '')).toBe('a &lt; b')
  })

  it('escapes markup inside highlighted code', () => {
    const html = highlightCode('const s = "<b>";', 'javascript')

    expect(html).not.toContain('<b>')
    expect(html).toContain('&lt;b&gt;')
  })
})
