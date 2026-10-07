import type { MarkDto, PaletteColor, SpanDto } from '@/entities/block'
import { mergeAdjacentSpans } from './spans'

/**
 * Caret positions are tracked as plain-text offsets (the concatenation of all
 * span texts), which is the same space `splitSpansAt` operates in.
 */
export function getCaretOffset(root: HTMLElement): number {
  const selection = window.getSelection()
  if (selection === null || selection.rangeCount === 0) {
    return 0
  }
  const range = selection.getRangeAt(0)
  if (!root.contains(range.endContainer)) {
    return 0
  }
  const before = range.cloneRange()
  before.selectNodeContents(root)
  before.setEnd(range.endContainer, range.endOffset)
  return before.toString().length
}

export function setCaretOffset(root: HTMLElement, offset: number): void {
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT)
  let remaining = offset
  let lastText: Node | null = null
  let node = walker.nextNode()

  while (node !== null) {
    const length = node.textContent?.length ?? 0
    if (remaining <= length) {
      placeCaret(node, remaining)
      return
    }
    remaining -= length
    lastText = node
    node = walker.nextNode()
  }

  // Past the end: land after the last text node (an empty block gets offset 0).
  if (lastText !== null) {
    placeCaret(lastText, lastText.textContent?.length ?? 0)
  } else {
    placeCaret(root, 0)
  }
}

function placeCaret(node: Node, offset: number): void {
  const range = document.createRange()
  range.setStart(node, offset)
  range.collapse(true)
  const selection = window.getSelection()
  selection?.removeAllRanges()
  selection?.addRange(range)
}

/**
 * Reads a contenteditable's inline DOM back into spans. Only the elements the
 * editor itself renders are recognised; anything else contributes its text
 * with the marks accumulated so far.
 */
export function parseEditableDom(root: HTMLElement): SpanDto[] {
  const spans: SpanDto[] = []

  const walk = (node: Node, marks: readonly MarkDto[]): void => {
    if (node.nodeType === Node.TEXT_NODE) {
      const text = node.textContent ?? ''
      if (text.length > 0) {
        spans.push({ text, marks: [...marks] })
      }
      return
    }
    if (node.nodeType !== Node.ELEMENT_NODE) {
      return
    }
    const element = node as HTMLElement
    if (element.tagName === 'BR') {
      return
    }
    const nextMarks = [...marks, ...marksOfElement(element)]
    for (const child of Array.from(element.childNodes)) {
      walk(child, nextMarks)
    }
  }

  for (const child of Array.from(root.childNodes)) {
    walk(child, [])
  }

  return mergeAdjacentSpans(spans)
}

function marksOfElement(element: HTMLElement): MarkDto[] {
  switch (element.tagName) {
    case 'STRONG':
      return [{ kind: 'bold' }]
    case 'EM':
      return [{ kind: 'italic' }]
    case 'U':
      return [{ kind: 'underline' }]
    case 'S':
      return [{ kind: 'strike' }]
    case 'CODE':
      return [{ kind: 'code' }]
    case 'KBD':
      return [{ kind: 'kbd' }]
    case 'SUP':
      return [{ kind: 'sup' }]
    case 'SUB':
      return [{ kind: 'sub' }]
    case 'A': {
      const href = element.getAttribute('href')
      return href !== null ? [{ kind: 'link', href }] : []
    }
    case 'ABBR': {
      const title = element.getAttribute('title')
      return title !== null ? [{ kind: 'abbr', title }] : []
    }
    default: {
      const color = element.getAttribute('data-color')
      if (color !== null) {
        return [{ kind: 'color', name: color as PaletteColor }]
      }
      const highlight = element.getAttribute('data-highlight')
      if (highlight !== null) {
        return [{ kind: 'highlight', name: highlight as PaletteColor }]
      }
      return []
    }
  }
}
