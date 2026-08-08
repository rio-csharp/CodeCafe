import { describe, expect, it } from 'vitest'
import { formatTimeAgo } from './timeAgo'

describe('formatTimeAgo', () => {
  it('returns an empty string for invalid dates instead of a misleading "now"', () => {
    expect(formatTimeAgo('not-a-date')).toBe('')
    expect(formatTimeAgo('')).toBe('')
  })

  it('formats a current timestamp as now', () => {
    expect(formatTimeAgo(new Date().toISOString(), 'en')).toBe('now')
  })

  it('formats past dates relative to now', () => {
    const twoHoursAgo = new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString()
    expect(formatTimeAgo(twoHoursAgo, 'en')).toMatch(/2 .*\bagos?$|2 .* ago/)
  })
})
