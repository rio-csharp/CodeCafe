import { describe, expect, it } from 'vitest'
import { isPlaceholderSlug, pageSlug, slugify } from './pageSlug'

describe('pageSlug', () => {
  it('takes the segment that names the page', () => {
    expect(pageSlug('/setup/rust-notes')).toBe('rust-notes')
    expect(pageSlug('/untitled-4821')).toBe('untitled-4821')
  })

  it('copes with a path that carries no segment', () => {
    expect(pageSlug('/')).toBe('')
    expect(pageSlug('')).toBe('')
  })
})

describe('slugify', () => {
  it('mirrors the server rule', () => {
    expect(slugify('Getting Started')).toBe('getting-started')
    expect(slugify('  Rust   &   Notes  ')).toBe('rust-notes')
    // Unicode letters survive: a CJK title keeps a readable slug.
    expect(slugify('咖啡笔记')).toBe('咖啡笔记')
    expect(slugify('Café Notes')).toBe('café-notes')
  })

  it('collapses a title with nothing slug-worthy', () => {
    expect(slugify('!!!')).toBe('')
  })

  it('caps the length the way the column does, without a dangling hyphen', () => {
    // The cap lands inside the tail: the cut simply keeps the first 80 characters.
    expect(slugify(`${'a'.repeat(70)} ${'b'.repeat(20)}`)).toHaveLength(80)
    // The cap lands right on a hyphen: it is trimmed rather than left dangling.
    expect(slugify(`${'a'.repeat(79)} rest`)).toBe('a'.repeat(79))
    expect(slugify('a'.repeat(90))).toBe('a'.repeat(80))
  })
})

describe('isPlaceholderSlug', () => {
  it('recognises the URL a nameless page was created with', () => {
    expect(isPlaceholderSlug('untitled', 'Untitled')).toBe(true)
    expect(isPlaceholderSlug('untitled-4821', 'Untitled')).toBe(true)
    expect(isPlaceholderSlug('未命名', '未命名')).toBe(true)
    expect(isPlaceholderSlug('未命名-4821', '未命名')).toBe(true)
  })

  it('rejects a slug that was derived from a real title', () => {
    expect(isPlaceholderSlug('grinding', 'Untitled')).toBe(false)
    expect(isPlaceholderSlug('untitled-notes', 'Untitled')).toBe(false)
    expect(isPlaceholderSlug('untitled-abc', 'Untitled')).toBe(false)
  })

  it('rejects everything when the placeholder itself has no slug', () => {
    expect(isPlaceholderSlug('untitled', '!!!')).toBe(false)
  })
})
