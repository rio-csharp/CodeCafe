import { describe, expect, it } from 'vitest'
import { formatRelativeTime } from './formatRelativeTime'

const NOW = Date.parse('2026-01-10T12:00:00.000Z')
const THREE_DAYS_AGO = '2026-01-07T12:00:00.000Z'

describe('formatRelativeTime', () => {
  it('formats in English', () => {
    expect(formatRelativeTime(THREE_DAYS_AGO, 'en', NOW)).toBe('3 days ago')
  })

  it('formats in Chinese', () => {
    expect(formatRelativeTime(THREE_DAYS_AGO, 'zh', NOW)).toBe('3天前')
  })

  it('uses the closest unit', () => {
    expect(formatRelativeTime('2026-01-10T11:30:00.000Z', 'en', NOW)).toBe('30 minutes ago')
  })

  it('returns an empty string for an unparseable date', () => {
    expect(formatRelativeTime('not-a-date', 'en', NOW)).toBe('')
  })
})
