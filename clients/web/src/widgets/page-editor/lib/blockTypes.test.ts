import { describe, expect, it } from 'vitest'
import { contentForTarget, detectSlashQuery, filterSlashItems } from './blockTypes'

describe('detectSlashQuery', () => {
  it('starts a query on a leading slash and ends it on whitespace', () => {
    expect(detectSlashQuery('/hea')).toBe('hea')
    expect(detectSlashQuery('/')).toBe('')
    expect(detectSlashQuery('hello')).toBeNull()
    expect(detectSlashQuery('/heading one')).toBeNull()
    expect(detectSlashQuery('not / at start')).toBeNull()
  })
})

describe('filterSlashItems', () => {
  it('returns everything for an empty query', () => {
    expect(filterSlashItems('')).toHaveLength(10)
  })

  it('matches ids and keywords case-insensitively', () => {
    expect(filterSlashItems('hea').map((item) => item.id)).toEqual([
      'heading1',
      'heading2',
      'heading3',
    ])
    expect(filterSlashItems('H2').map((item) => item.id)).toEqual(['heading2'])
  })

  it('matches Chinese keywords too', () => {
    expect(filterSlashItems('标题').map((item) => item.id)).toEqual([
      'heading1',
      'heading2',
      'heading3',
    ])
    expect(filterSlashItems('分割').map((item) => item.id)).toEqual(['divider'])
  })
})

describe('contentForTarget', () => {
  it('shapes content per target type', () => {
    const spans = [{ text: 'hi', marks: [] }]

    expect(contentForTarget({ type: 'heading', level: 2 }, spans)).toEqual({ level: 2, spans })
    expect(contentForTarget({ type: 'todo' }, spans)).toEqual({ checked: false, spans })
    expect(contentForTarget({ type: 'callout' }, spans)).toEqual({ variant: 'info', spans })
    expect(contentForTarget({ type: 'divider' }, spans)).toEqual({})
  })
})
