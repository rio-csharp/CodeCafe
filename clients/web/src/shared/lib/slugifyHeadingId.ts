/**
 * Single source of truth for heading anchor ids.
 *
 * Contract shared by extractOutline (outline panel) and TipTapViewer
 * (rendered headings): only headings with non-empty text receive an id, and
 * `index` counts only those headings. Both callers must follow this rule or
 * outline anchors drift off-by-one.
 */
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
