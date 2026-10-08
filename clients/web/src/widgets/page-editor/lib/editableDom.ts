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
  return offsetAtPoint(root, range.endContainer, range.endOffset)
}

/** Selection endpoints as plain-text offsets; null when outside the block. */
export function getSelectionOffsets(root: HTMLElement): { start: number; end: number } | null {
  const selection = window.getSelection()
  if (
    selection === null ||
    selection.anchorNode === null ||
    selection.focusNode === null ||
    !root.contains(selection.anchorNode) ||
    !root.contains(selection.focusNode)
  ) {
    return null
  }
  const anchor = offsetAtPoint(root, selection.anchorNode, selection.anchorOffset)
  const focus = offsetAtPoint(root, selection.focusNode, selection.focusOffset)
  return { start: Math.min(anchor, focus), end: Math.max(anchor, focus) }
}

/**
 * Selection endpoints as plain-text offsets, clamped to this block. Unlike
 * getSelectionOffsets, a drag that crosses the block boundary still counts —
 * the caller acts on the in-block portion (e.g. the floating toolbar). Null
 * when the selection does not overlap the block at all.
 */
export function getClampedSelectionOffsets(
  root: HTMLElement,
): { start: number; end: number } | null {
  const selection = window.getSelection()
  if (selection === null || selection.rangeCount === 0 || selection.isCollapsed) {
    return null
  }
  const selected = selection.getRangeAt(0)
  const block = document.createRange()
  block.selectNodeContents(root)
  // No overlap: the selection ends at or before the block starts, or starts
  // at or after the block ends.
  if (
    selected.compareBoundaryPoints(Range.START_TO_END, block) <= 0 ||
    selected.compareBoundaryPoints(Range.END_TO_START, block) >= 0
  ) {
    return null
  }
  const length = root.textContent?.length ?? 0
  const start =
    selected.compareBoundaryPoints(Range.START_TO_START, block) <= 0
      ? 0
      : offsetAtPoint(root, selected.startContainer, selected.startOffset)
  const end =
    selected.compareBoundaryPoints(Range.END_TO_END, block) >= 0
      ? length
      : offsetAtPoint(root, selected.endContainer, selected.endOffset)
  return start === end ? null : { start, end }
}

function offsetAtPoint(root: HTMLElement, node: Node, offset: number): number {
  const range = document.createRange()
  range.selectNodeContents(root)
  range.setEnd(node, offset)
  return range.toString().length
}

export function setCaretOffset(root: HTMLElement, offset: number): void {
  setSelectionOffsets(root, offset, offset)
}

/** Restore a (possibly non-collapsed) selection from plain-text offsets. */
export function setSelectionOffsets(root: HTMLElement, start: number, end: number): void {
  const anchor = locatePoint(root, start)
  const focus = locatePoint(root, end)
  const selection = window.getSelection()
  if (anchor === null || focus === null || selection === null) {
    return
  }
  selection.setBaseAndExtent(anchor.node, anchor.offset, focus.node, focus.offset)
}

/** The DOM range between two plain-text offsets, e.g. for measuring its rect. */
export function rangeForOffsets(root: HTMLElement, start: number, end: number): Range | null {
  const anchor = locatePoint(root, start)
  const focus = locatePoint(root, end)
  if (anchor === null || focus === null) {
    return null
  }
  const range = document.createRange()
  range.setStart(anchor.node, anchor.offset)
  range.setEnd(focus.node, focus.offset)
  return range
}

function locatePoint(root: HTMLElement, target: number): { node: Node; offset: number } | null {
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT)
  let remaining = target
  let lastText: Node | null = null
  let node = walker.nextNode()

  while (node !== null) {
    const length = node.textContent?.length ?? 0
    if (remaining <= length) {
      return { node, offset: remaining }
    }
    remaining -= length
    lastText = node
    node = walker.nextNode()
  }

  // Past the end: land after the last text node (an empty block gets offset 0).
  if (lastText !== null) {
    return { node: lastText, offset: lastText.textContent?.length ?? 0 }
  }
  return { node: root, offset: 0 }
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
