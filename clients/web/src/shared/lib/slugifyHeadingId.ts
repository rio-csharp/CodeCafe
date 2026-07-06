export function slugifyHeadingId(text: string, index: number): string {
  const slug = text
    .toLowerCase()
    .trim()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9\u4e00-\u9fa5-]/g, '')
    .substring(0, 60)
  if (!slug) {
    return `heading-${index}`
  }

  const base = `heading-${slug}`
  return index > 0 ? `${base}-${index}` : base
}
