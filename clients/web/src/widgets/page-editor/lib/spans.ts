import type { MarkDto, SpanDto } from '@/entities/block'

/** The caret/selection currency of the editor: plain-text offsets. */
export function spansPlainText(spans: readonly SpanDto[]): string {
  return spans.map((span) => span.text).join('')
}

export function marksKey(marks: readonly MarkDto[]): string {
  return JSON.stringify(marks)
}

/** Drops empty spans and folds neighbours with identical marks into one. */
export function mergeAdjacentSpans(spans: readonly SpanDto[]): SpanDto[] {
  const merged: SpanDto[] = []
  for (const span of spans) {
    if (span.text.length === 0) {
      continue
    }
    const last = merged[merged.length - 1]
    if (last !== undefined && marksKey(last.marks) === marksKey(span.marks)) {
      last.text += span.text
    } else {
      merged.push({ text: span.text, marks: [...span.marks] })
    }
  }
  return merged
}

/** Splits at a plain-text offset; either side may come back empty. */
export function splitSpansAt(
  spans: readonly SpanDto[],
  offset: number,
): [SpanDto[], SpanDto[]] {
  const left: SpanDto[] = []
  const right: SpanDto[] = []
  let consumed = 0

  for (const span of spans) {
    const end = consumed + span.text.length
    if (end <= offset) {
      left.push({ ...span, marks: [...span.marks] })
    } else if (consumed >= offset) {
      right.push({ ...span, marks: [...span.marks] })
    } else {
      left.push({ text: span.text.slice(0, offset - consumed), marks: [...span.marks] })
      right.push({ text: span.text.slice(offset - consumed), marks: [...span.marks] })
    }
    consumed = end
  }

  return [mergeAdjacentSpans(left), mergeAdjacentSpans(right)]
}

export function joinSpans(left: readonly SpanDto[], right: readonly SpanDto[]): SpanDto[] {
  return mergeAdjacentSpans([...left, ...right])
}

export function insertTextAt(
  spans: readonly SpanDto[],
  offset: number,
  text: string,
): SpanDto[] {
  const [left, right] = splitSpansAt(spans, offset)
  // Inherit the left neighbour's marks so typing inside a bold run stays bold.
  const marks = left.length > 0 ? [...left[left.length - 1]!.marks] : []
  return mergeAdjacentSpans([...left, { text, marks }, ...right])
}

/** Replaces a plain-text range and inherits the nearest surrounding marks. */
export function replaceTextRange(
  spans: readonly SpanDto[],
  start: number,
  end: number,
  text: string,
): SpanDto[] {
  const from = Math.max(0, Math.min(start, end))
  const to = Math.max(from, Math.max(start, end))
  const [left, rest] = splitSpansAt(spans, from)
  const [, right] = splitSpansAt(rest, to - from)
  const marks = [...(left[left.length - 1]?.marks ?? right[0]?.marks ?? [])]
  return mergeAdjacentSpans([...left, { text, marks }, ...right])
}

/** Value equality, marks included; used to tell external edits from local ones. */
export function spansEqual(a: readonly SpanDto[], b: readonly SpanDto[]): boolean {
  return (
    a.length === b.length &&
    a.every((span, index) => {
      const other = b[index]!
      return span.text === other.text && marksKey(span.marks) === marksKey(other.marks)
    })
  )
}
