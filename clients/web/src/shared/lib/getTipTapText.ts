interface TipTapNode {
  type?: string
  text?: string
  content?: TipTapNode[]
}

// Node types whose inline content forms its own line (TipTap "textblocks").
const TEXT_BLOCK_TYPES = new Set(['paragraph', 'heading', 'codeBlock'])

function inlineText(node: TipTapNode): string {
  if (node.type === 'text') return node.text ?? ''
  if (node.type === 'hardBreak') return '\n'
  return (node.content ?? []).map(inlineText).join('')
}

function collectLines(node: TipTapNode, lines: string[]): void {
  if (TEXT_BLOCK_TYPES.has(node.type ?? '')) {
    lines.push(inlineText(node))
    return
  }
  ;(node.content ?? []).forEach((child) => collectLines(child, lines))
}

/**
 * Extract plain text from TipTap JSON by pure traversal — instantiating a
 * full TipTap Editor per call (as this used to) builds ~40 extensions each
 * time, which is too costly for list/preview scenarios. Blocks are joined
 * with '\n', matching `editor.getText({ blockSeparator: '\n' })`.
 */
export function getTipTapText(content: Record<string, unknown> | null | undefined): string {
  if (!content || typeof content !== 'object') return ''
  const lines: string[] = []
  collectLines(content as TipTapNode, lines)
  return lines.join('\n')
}
