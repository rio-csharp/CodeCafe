const DIVISIONS: readonly { amount: number; unit: Intl.RelativeTimeFormatUnit }[] = [
  { amount: 60, unit: 'second' },
  { amount: 60, unit: 'minute' },
  { amount: 24, unit: 'hour' },
  { amount: 7, unit: 'day' },
  { amount: 4.34524, unit: 'week' },
  { amount: 12, unit: 'month' },
  { amount: Number.POSITIVE_INFINITY, unit: 'year' },
]

/**
 * Locale-aware "3 days ago" / "3天前". `now` is injectable so tests stay deterministic.
 */
export function formatRelativeTime(isoDate: string, locale: string, now: number = Date.now()): string {
  const timestamp = Date.parse(isoDate)
  if (Number.isNaN(timestamp)) {
    return ''
  }

  const formatter = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' })
  let duration = (timestamp - now) / 1000

  for (const division of DIVISIONS) {
    if (Math.abs(duration) < division.amount) {
      return formatter.format(Math.round(duration), division.unit)
    }
    duration /= division.amount
  }

  return formatter.format(Math.round(duration), 'year')
}
