type RelativeUnit = Intl.RelativeTimeFormatUnit

const DIVISIONS: Array<{ amount: number; unit: RelativeUnit }> = [
  { amount: 60, unit: 'second' },
  { amount: 60, unit: 'minute' },
  { amount: 24, unit: 'hour' },
  { amount: 30, unit: 'day' },
  { amount: 12, unit: 'month' },
  { amount: Number.POSITIVE_INFINITY, unit: 'year' },
]

export function formatTimeAgo(iso: string, locale?: string): string {
  const ts = new Date(iso).getTime()
  const formatter = new Intl.RelativeTimeFormat(locale, {
    numeric: 'auto',
    style: 'short',
  })

  if (Number.isNaN(ts)) return formatter.format(0, 'second')

  let value = Math.round((ts - Date.now()) / 1000)
  let unit: RelativeUnit = 'second'

  for (const division of DIVISIONS) {
    unit = division.unit
    if (Math.abs(value) < division.amount) break
    value = Math.round(value / division.amount)
  }

  return formatter.format(value, unit)
}
