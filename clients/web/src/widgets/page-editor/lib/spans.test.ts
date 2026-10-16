import { describe, expect, it } from 'vitest'
import type { SpanDto } from '@/entities/block'
import { insertTextAt, joinSpans, mergeAdjacentSpans, splitSpansAt } from './spans'

const plain = (text: string): SpanDto => ({ text, marks: [] })
const bold = (text: string): SpanDto => ({ text, marks: [{ kind: 'bold' }] })

describe('splitSpansAt', () => {
  it('splits inside a span, keeping its marks on both sides', () => {
    const [left, right] = splitSpansAt([bold('hello')], 2)

    expect(left).toEqual([bold('he')])
    expect(right).toEqual([bold('llo')])
  })

  it('splits on a span boundary without inventing empty spans', () => {
    const [left, right] = splitSpansAt([plain('ab'), bold('cd')], 2)

    expect(left).toEqual([plain('ab')])
    expect(right).toEqual([bold('cd')])
  })
})

describe('joinSpans', () => {
  it('folds the junction when both sides carry the same marks', () => {
    expect(joinSpans([bold('he')], [bold('llo')])).toEqual([bold('hello')])
  })

  it('keeps the junction when the marks differ', () => {
    expect(joinSpans([plain('a')], [bold('b')])).toEqual([plain('a'), bold('b')])
  })
})

describe('insertTextAt', () => {
  it('inherits the marks of the text on its left', () => {
    expect(insertTextAt([bold('ab')], 2, '!')).toEqual([bold('ab!')])
  })

  it('inserts unmarked into plain text', () => {
    expect(insertTextAt([plain('ac')], 1, 'b')).toEqual([plain('abc')])
  })
})

describe('mergeAdjacentSpans', () => {
  it('drops empty spans', () => {
    expect(mergeAdjacentSpans([plain(''), plain('x')])).toEqual([plain('x')])
  })
})
