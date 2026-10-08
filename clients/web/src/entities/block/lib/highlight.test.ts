import { describe, expect, it } from 'vitest'
import { highlightCode, HIGHLIGHT_LANGUAGES, isHighlightableLanguage } from './highlight'

describe('highlightCode', () => {
  it('highlights C# under its canonical name and aliases', () => {
    for (const language of ['csharp', 'c#', 'cs', 'C#']) {
      expect(highlightCode('public class Foo {}', language)).toContain('hljs-keyword')
      expect(isHighlightableLanguage(language)).toBe(true)
    }
  })

  it('escapes unknown or empty languages as plain text', () => {
    expect(highlightCode('<b>x</b>', 'not-a-lang')).toBe('&lt;b&gt;x&lt;/b&gt;')
    expect(highlightCode('<b>x</b>', '')).toBe('&lt;b&gt;x&lt;/b&gt;')
    expect(isHighlightableLanguage('not-a-lang')).toBe(false)
  })

  it('exposes the bundled language list for pickers', () => {
    expect(HIGHLIGHT_LANGUAGES).toContain('csharp')
    expect(HIGHLIGHT_LANGUAGES).toContain('typescript')
  })
})
