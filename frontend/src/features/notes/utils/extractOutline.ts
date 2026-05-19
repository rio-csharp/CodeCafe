export interface OutlineHeading {
  id: string
  level: number
  text: string
}

function getTextFromNode(node: unknown): string {
  if (typeof node !== 'object' || node === null) return ''
  const n = node as Record<string, unknown>
  if (n.type === 'text' && typeof n.text === 'string') {
    const text = n.text
    if (Array.isArray(n.marks)) {
      // marks don't affect plain text extraction for outline
    }
    return text
  }
  if (Array.isArray(n.content)) {
    return n.content.map(getTextFromNode).join('')
  }
  return ''
}

export function slugifyHeadingId(text: string, index: number): string {
  const slug = text
    .toLowerCase()
    .trim()
    .replace(/\s+/g, '-')
    .replace(/[^a-z0-9\u4e00-\u9fa5-]/g, '')
    .substring(0, 60)
  return slug ? `heading-${slug}` : `heading-${index}`
}

export function extractOutline(contentJson: Record<string, unknown> | null): OutlineHeading[] {
  if (!contentJson) return []
  const headings: OutlineHeading[] = []
  const doc = contentJson

  function walk(node: unknown) {
    if (typeof node !== 'object' || node === null) return
    const n = node as Record<string, unknown>
    if (n.type === 'heading' && typeof (n.attrs as Record<string, unknown>)?.level === 'number') {
      const level = (n.attrs as Record<string, unknown>).level as number
      const text = getTextFromNode(node)
      if (text) {
        const id = slugifyHeadingId(text, headings.length)
        headings.push({ id, level, text })
      }
    }
    if (Array.isArray(n.content)) {
      n.content.forEach(walk)
    }
  }

  walk(doc)
  return headings
}
