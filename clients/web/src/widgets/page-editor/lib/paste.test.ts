import { describe, expect, it } from 'vitest'
import { parsePastedBlocks, pastedPlainText } from './paste'

describe('parsePastedBlocks', () => {
  it('returns null for single-line text so the caller inserts it in place', () => {
    expect(parsePastedBlocks('just one line')).toBeNull()
  })

  it('returns null when every line is blank', () => {
    expect(parsePastedBlocks('\n\n')).toBeNull()
  })

  it('splits plain lines into paragraphs and skips blank lines', () => {
    expect(parsePastedBlocks('one\n\ntwo')).toEqual([
      { type: 'paragraph', content: { spans: [{ text: 'one', marks: [] }] } },
      { type: 'paragraph', content: { spans: [{ text: 'two', marks: [] }] } },
    ])
  })

  it('parses markdown headings with their level', () => {
    expect(parsePastedBlocks('## Brew guide\nbody')).toEqual([
      { type: 'heading', content: { level: 2, spans: [{ text: 'Brew guide', marks: [] }] } },
      { type: 'paragraph', content: { spans: [{ text: 'body', marks: [] }] } },
    ])
  })

  it('parses to-dos, bullets and numbered items', () => {
    expect(parsePastedBlocks('- [ ] buy beans\n- [x] grind\n- plain\n1. first')).toEqual([
      { type: 'todo', content: { checked: false, spans: [{ text: 'buy beans', marks: [] }] } },
      { type: 'todo', content: { checked: true, spans: [{ text: 'grind', marks: [] }] } },
      { type: 'bulleted-list', content: { spans: [{ text: 'plain', marks: [] }] } },
      { type: 'numbered-list', content: { spans: [{ text: 'first', marks: [] }] } },
    ])
  })

  it('parses quotes and dividers', () => {
    expect(parsePastedBlocks('> slow down\n---')).toEqual([
      { type: 'quote', content: { spans: [{ text: 'slow down', marks: [] }] } },
      { type: 'divider', content: {} },
    ])
  })

  it('collects fenced code, language included, and tolerates a missing close', () => {
    expect(parsePastedBlocks('```rust\nfn main() {}\nlet x = 1;\n```\nafter')).toEqual([
      { type: 'code', content: { code: 'fn main() {}\nlet x = 1;', language: 'rust' } },
      { type: 'paragraph', content: { spans: [{ text: 'after', marks: [] }] } },
    ])

    expect(parsePastedBlocks('```\nunclosed')).toEqual([
      { type: 'code', content: { code: 'unclosed', language: '' } },
    ])
  })

  it('keeps markdown-looking text literal when it is not at line start', () => {
    expect(parsePastedBlocks('a # not-a-heading\nb')).toEqual([
      { type: 'paragraph', content: { spans: [{ text: 'a # not-a-heading', marks: [] }] } },
      { type: 'paragraph', content: { spans: [{ text: 'b', marks: [] }] } },
    ])
  })
})

describe('pastedPlainText', () => {
  it('reads the text of span-carrying blocks and rejects the rest', () => {
    expect(
      pastedPlainText({ type: 'paragraph', content: { spans: [{ text: 'hi', marks: [] }] } }),
    ).toBe('hi')
    expect(pastedPlainText({ type: 'divider', content: {} })).toBeNull()
  })
})
