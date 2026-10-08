import type { SpanDto } from '@/entities/block'

/** A block produced from pasted text: type plus a wire-ready content payload. */
export interface PastedBlock {
  type: string
  content: Record<string, unknown>
}

const textSpans = (text: string): SpanDto[] => (text.length === 0 ? [] : [{ text, marks: [] }])

const FENCE_OPEN = /^```(\S*)\s*$/
const FENCE_CLOSE = /^```\s*$/
const HEADING = /^(#{1,6})\s+(.+)$/
const TODO = /^[-*]\s+\[([ xX])\]\s+(.*)$/
const BULLET = /^[-*]\s+(.+)$/
const NUMBERED = /^\d+[.)]\s+(.+)$/
const QUOTE = /^>\s?(.*)$/
const DIVIDER = /^\s*(?:-{3,}|\*{3,}|_{3,})\s*$/

/**
 * Turns pasted plain text into blocks. Single-line text returns null — the
 * caller falls back to inserting it into the current block. Multi-line text
 * becomes one block per line with Markdown line syntax honoured (headings,
 * lists, to-dos, quotes, dividers, fenced code); inline marks are NOT parsed,
 * pasted text carries no formatting.
 */
export function parsePastedBlocks(text: string): PastedBlock[] | null {
  if (!text.includes('\n')) {
    return null
  }

  const lines = text.split('\n')
  const blocks: PastedBlock[] = []
  let index = 0
  while (index < lines.length) {
    const line = lines[index]!
    index += 1

    const fence = FENCE_OPEN.exec(line)
    if (fence !== null) {
      const codeLines: string[] = []
      while (index < lines.length && !FENCE_CLOSE.test(lines[index]!)) {
        codeLines.push(lines[index]!)
        index += 1
      }
      index += 1 // consume the closing fence (a no-op at end of input)
      blocks.push({
        type: 'code',
        content: { code: codeLines.join('\n'), language: fence[1] ?? '' },
      })
      continue
    }

    if (line.trim().length === 0) {
      // Blank lines separate blocks; they do not become empty paragraphs.
      continue
    }

    const heading = HEADING.exec(line)
    if (heading !== null) {
      blocks.push({
        type: 'heading',
        content: { level: heading[1]!.length, spans: textSpans(heading[2]!) },
      })
      continue
    }

    const todo = TODO.exec(line)
    if (todo !== null) {
      blocks.push({
        type: 'todo',
        content: { checked: todo[1]!.toLowerCase() === 'x', spans: textSpans(todo[2]!) },
      })
      continue
    }

    const bullet = BULLET.exec(line)
    if (bullet !== null) {
      blocks.push({ type: 'bulleted-list', content: { spans: textSpans(bullet[1]!) } })
      continue
    }

    const numbered = NUMBERED.exec(line)
    if (numbered !== null) {
      blocks.push({ type: 'numbered-list', content: { spans: textSpans(numbered[1]!) } })
      continue
    }

    const quote = QUOTE.exec(line)
    if (quote !== null) {
      blocks.push({ type: 'quote', content: { spans: textSpans(quote[1]!) } })
      continue
    }

    if (DIVIDER.test(line)) {
      blocks.push({ type: 'divider', content: {} })
      continue
    }

    blocks.push({ type: 'paragraph', content: { spans: textSpans(line) } })
  }

  return blocks.length === 0 ? null : blocks
}

/** The plain text of a pasted block, when the block carries text at all. */
export function pastedPlainText(block: PastedBlock): string | null {
  const spans = (block.content as { spans?: SpanDto[] }).spans
  return spans === undefined ? null : spans.map((span) => span.text).join('')
}
