import { slugifyHeadingId } from '@/shared/lib'

export { slugifyHeadingId }

export interface OutlineHeading {
  id: string
  level: number
  text: string
}

interface TipTapNode {
  type?: string
  text?: string
  attrs?: Record<string, unknown>
  content?: TipTapNode[]
}

function isTipTapNode(node: unknown): node is TipTapNode {
  return typeof node === 'object' && node !== null
}

function getTextFromNode(node: TipTapNode): string {
  if (node.type === 'text' && typeof node.text === 'string') {
    return node.text
  }
  if (Array.isArray(node.content)) {
    return node.content.map(getTextFromNode).join('')
  }
  return ''
}

export function extractOutline(contentJson: Record<string, unknown> | null): OutlineHeading[] {
  if (!contentJson || !isTipTapNode(contentJson)) return []
  const headings: OutlineHeading[] = []

  function walk(node: TipTapNode) {
    if (node.type === 'heading' && typeof node.attrs?.level === 'number') {
      const text = getTextFromNode(node)
      // Empty headings are skipped — and do not consume an id index — to
      // stay aligned with TipTapViewer (see slugifyHeadingId's contract).
      if (text) {
        const id = slugifyHeadingId(text, headings.length)
        headings.push({ id, level: node.attrs.level, text })
      }
    }
    if (Array.isArray(node.content)) {
      node.content.forEach((child) => {
        if (isTipTapNode(child)) walk(child)
      })
    }
  }

  walk(contentJson)
  return headings
}
