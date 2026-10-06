import type { BlockNode } from './assembleBlockTree'
import type { HeadingContent, SpanDto } from './types'

export interface OutlineHeading {
  /** The block id doubles as the anchor — stable, unlike a slugified title. */
  id: string
  level: number
  text: string
}

export function blockAnchorId(blockId: string): string {
  return `block-${blockId}`
}

/**
 * The page's heading blocks in document order. Extracted from the assembled
 * tree rather than the flat list so nested blocks stay where they read.
 */
export function extractOutline(nodes: readonly BlockNode[]): OutlineHeading[] {
  const headings: OutlineHeading[] = []

  const walk = (list: readonly BlockNode[]): void => {
    for (const node of list) {
      const { block } = node

      if (block.type === 'heading') {
        const content = block.content as Partial<HeadingContent> | null
        const text = plainText(content?.spans ?? [])
        if (text.trim().length > 0) {
          headings.push({
            id: blockAnchorId(block.id),
            level: Math.min(Math.max(Math.trunc(content?.level ?? 1) || 1, 1), 6),
            text,
          })
        }
      }

      walk(node.children)
    }
  }

  walk(nodes)
  return headings
}

function plainText(spans: readonly SpanDto[]): string {
  return spans.map((span) => span.text).join('')
}
