import type { MarkDto, SpanDto } from '@/entities/block'
import { mergeAdjacentSpans, splitSpansAt } from './spans'

/** Marks the floating toolbar can toggle; link/color/highlight carry payloads. */
export type SimpleMarkKind = 'bold' | 'italic' | 'underline' | 'strike' | 'code'

function slice3(
  spans: readonly SpanDto[],
  start: number,
  end: number,
): [SpanDto[], SpanDto[], SpanDto[]] {
  const [before, rest] = splitSpansAt(spans, start)
  const [middle, after] = splitSpansAt(rest, end - start)
  return [before, middle, after]
}

/** True when every character of [start, end) already carries the mark. */
export function rangeHasMark(
  spans: readonly SpanDto[],
  start: number,
  end: number,
  kind: MarkDto['kind'],
): boolean {
  if (end <= start) {
    return false
  }
  const [, middle] = slice3(spans, start, end)
  return middle.length > 0 && middle.every((span) => span.marks.some((m) => m.kind === kind))
}

/** The set of mark kinds active across the whole selection; drives button states. */
export function rangeMarks(
  spans: readonly SpanDto[],
  start: number,
  end: number,
): Set<MarkDto['kind']> {
  const kinds: Array<MarkDto['kind']> = [
    'bold',
    'italic',
    'underline',
    'strike',
    'code',
    'link',
    'color',
    'highlight',
  ]
  return new Set(kinds.filter((kind) => rangeHasMark(spans, start, end, kind)))
}

/** Toggle a simple mark over [start, end): remove when fully marked, else add. */
export function toggleMark(
  spans: readonly SpanDto[],
  start: number,
  end: number,
  kind: SimpleMarkKind,
): SpanDto[] {
  if (end <= start) {
    return mergeAdjacentSpans(spans)
  }
  const remove = rangeHasMark(spans, start, end, kind)
  const [before, middle, after] = slice3(spans, start, end)
  const changed = middle.map((span) => ({
    ...span,
    marks: remove
      ? span.marks.filter((m) => m.kind !== kind)
      : ([...span.marks.filter((m) => m.kind !== kind), { kind }] as MarkDto[]),
  }))
  return mergeAdjacentSpans([...before, ...changed, ...after])
}

/**
 * The backend only accepts absolute http/https/mailto hrefs, so bare domains
 * typed into the link box get https:// prepended.
 */
export function normalizeHref(href: string): string {
  const trimmed = href.trim()
  if (/^(https?:\/\/|mailto:)/i.test(trimmed)) {
    return trimmed
  }
  return `https://${trimmed}`
}

/** The first link href inside [start, end), if any — prefills the link box. */
export function linkHrefInRange(
  spans: readonly SpanDto[],
  start: number,
  end: number,
): string | null {
  if (end <= start) {
    return null
  }
  const [, middle] = slice3(spans, start, end)
  for (const span of middle) {
    const link = span.marks.find((m) => m.kind === 'link')
    if (link !== undefined && link.kind === 'link') {
      return link.href
    }
  }
  return null
}

/** Add or replace the link over [start, end); null href removes it. */
export function applyLink(
  spans: readonly SpanDto[],
  start: number,
  end: number,
  href: string | null,
): SpanDto[] {
  if (end <= start) {
    return mergeAdjacentSpans(spans)
  }
  const [before, middle, after] = slice3(spans, start, end)
  const changed = middle.map((span) => ({
    ...span,
    marks:
      href === null
        ? span.marks.filter((m) => m.kind !== 'link')
        : ([...span.marks.filter((m) => m.kind !== 'link'), { kind: 'link', href }] as MarkDto[]),
  }))
  return mergeAdjacentSpans([...before, ...changed, ...after])
}
