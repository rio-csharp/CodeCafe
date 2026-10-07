import { describe, expect, it } from 'vitest'
import type { SpanDto } from '@/entities/block'
import { applyLink, linkHrefInRange, normalizeHref, rangeHasMark, rangeMarks, toggleMark } from './marks'

const plain = (text: string): SpanDto => ({ text, marks: [] })
const bold = (text: string): SpanDto => ({ text, marks: [{ kind: 'bold' }] })

describe('toggleMark', () => {
  it('marks a subrange that cuts across span boundaries', () => {
    const result = toggleMark([plain('hello '), bold('world')], 3, 8, 'italic')

    expect(result).toEqual([
      plain('hel'),
      { text: 'lo ', marks: [{ kind: 'italic' }] },
      { text: 'wo', marks: [{ kind: 'bold' }, { kind: 'italic' }] },
      bold('rld'),
    ])
  })

  it('removes the mark when the whole range already has it', () => {
    expect(toggleMark([bold('hey')], 0, 3, 'bold')).toEqual([plain('hey')])
  })

  it('adds the mark when only part of the range has it', () => {
    const result = toggleMark([bold('he'), plain('y')], 0, 3, 'bold')

    expect(result).toEqual([bold('hey')])
  })

  it('is a no-op on a collapsed range', () => {
    expect(toggleMark([plain('hey')], 1, 1, 'bold')).toEqual([plain('hey')])
  })
})

describe('rangeHasMark / rangeMarks', () => {
  it('reports partial coverage as inactive', () => {
    const spans = [bold('he'), plain('y')]

    expect(rangeHasMark(spans, 0, 3, 'bold')).toBe(false)
    expect(rangeHasMark(spans, 0, 2, 'bold')).toBe(true)
  })

  it('collects every fully-covered kind', () => {
    const spans: SpanDto[] = [
      { text: 'ab', marks: [{ kind: 'bold' }, { kind: 'italic' }] },
      { text: 'cd', marks: [{ kind: 'bold' }] },
    ]

    expect(rangeMarks(spans, 0, 4)).toEqual(new Set(['bold']))
  })
})

describe('applyLink', () => {
  it('adds a link mark over the range', () => {
    expect(applyLink([plain('click here')], 6, 10, 'https://x.test')).toEqual([
      plain('click '),
      { text: 'here', marks: [{ kind: 'link', href: 'https://x.test' }] },
    ])
  })

  it('replaces an existing href instead of stacking links', () => {
    const spans: SpanDto[] = [{ text: 'here', marks: [{ kind: 'link', href: 'https://a' }] }]

    expect(applyLink(spans, 0, 4, 'https://b')).toEqual([
      { text: 'here', marks: [{ kind: 'link', href: 'https://b' }] },
    ])
  })

  it('removes the link when href is null', () => {
    const spans: SpanDto[] = [{ text: 'here', marks: [{ kind: 'link', href: 'https://a' }] }]

    expect(applyLink(spans, 0, 4, null)).toEqual([plain('here')])
  })
})

describe('linkHrefInRange', () => {
  it('returns the href covering the selection, else null', () => {
    const spans: SpanDto[] = [
      plain('go '),
      { text: 'here', marks: [{ kind: 'link', href: 'https://a' }] },
    ]

    expect(linkHrefInRange(spans, 3, 7)).toBe('https://a')
    expect(linkHrefInRange(spans, 0, 2)).toBeNull()
  })
})

describe('normalizeHref', () => {
  it('prepends https:// to bare domains (the backend demands absolute URLs)', () => {
    expect(normalizeHref('codes.cafe')).toBe('https://codes.cafe')
  })

  it('leaves absolute hrefs and mailto alone', () => {
    expect(normalizeHref('http://x.test/a')).toBe('http://x.test/a')
    expect(normalizeHref('mailto:a@b.c')).toBe('mailto:a@b.c')
  })
})
