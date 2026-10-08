import { describe, expect, it } from 'vitest'
import { matchBacktickRule, matchSpaceRule } from './inputRules'

describe('matchSpaceRule', () => {
  it('converts hash markers to headings of the matching level', () => {
    expect(matchSpaceRule('#')).toEqual({ type: 'heading', level: 1 })
    expect(matchSpaceRule('##')).toEqual({ type: 'heading', level: 2 })
    expect(matchSpaceRule('###')).toEqual({ type: 'heading', level: 3 })
    expect(matchSpaceRule('####')).toBeNull()
  })

  it('converts list and quote markers', () => {
    expect(matchSpaceRule('-')).toEqual({ type: 'bulleted-list' })
    expect(matchSpaceRule('*')).toEqual({ type: 'bulleted-list' })
    expect(matchSpaceRule('1.')).toEqual({ type: 'numbered-list' })
    expect(matchSpaceRule('12)')).toEqual({ type: 'numbered-list' })
    expect(matchSpaceRule('>')).toEqual({ type: 'quote' })
  })

  it('converts to-do markers, checked state included', () => {
    expect(matchSpaceRule('[]')).toEqual({ type: 'todo' })
    expect(matchSpaceRule('[x]')).toEqual({ type: 'todo', checked: true })
    expect(matchSpaceRule('[X]')).toEqual({ type: 'todo', checked: true })
  })

  it('converts --- to a divider', () => {
    expect(matchSpaceRule('---')).toEqual({ type: 'divider' })
  })

  it('ignores markers that are not alone before the caret', () => {
    expect(matchSpaceRule('a #')).toBeNull()
    expect(matchSpaceRule('## b')).toBeNull()
    expect(matchSpaceRule('')).toBeNull()
    expect(matchSpaceRule('hello')).toBeNull()
  })
})

describe('matchBacktickRule', () => {
  it('converts on the third backtick only', () => {
    expect(matchBacktickRule('``')).toEqual({ type: 'code' })
    expect(matchBacktickRule('`')).toBeNull()
    expect(matchBacktickRule('a``')).toBeNull()
  })
})
