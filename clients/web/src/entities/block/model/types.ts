/**
 * Wire shapes for the block tree. Mirrors `server/CodeCafe.Domain/Blocks` and
 * `server/CodeCafe.Application/Blocks`: enums travel as lowercase strings and
 * `Spans` serializes as a flat array.
 */

/** The closed palette shared by callout variants and colour/highlight marks. */
export type PaletteColor = 'primary' | 'success' | 'danger' | 'warning' | 'info' | 'muted'

export type TableAlignment = 'none' | 'left' | 'center' | 'right'

export type MarkDto =
  | { kind: 'bold' }
  | { kind: 'italic' }
  | { kind: 'underline' }
  | { kind: 'strike' }
  | { kind: 'code' }
  | { kind: 'kbd' }
  | { kind: 'sup' }
  | { kind: 'sub' }
  | { kind: 'link'; href: string }
  | { kind: 'color'; name: PaletteColor }
  | { kind: 'highlight'; name: PaletteColor }
  | { kind: 'abbr'; title: string }

export interface SpanDto {
  text: string
  marks: MarkDto[]
}

export interface BlockDto {
  id: string
  parentBlockId: string | null
  type: string
  /** Per-type payload; every known shape lives in `BlockContent`. */
  content: unknown
  /** LexoRank ordinal — compare with `<`, never with locale collation. */
  sortKey: string
  version: number
  updatedAtUtc: string
}

export interface ParagraphContent {
  spans: SpanDto[]
}

export interface HeadingContent {
  level: number
  spans: SpanDto[]
}

export interface QuoteContent {
  spans: SpanDto[]
}

export interface CalloutContent {
  variant: PaletteColor
  spans: SpanDto[]
}

export interface TodoContent {
  checked: boolean
  spans: SpanDto[]
}

export interface ListItemContent {
  spans: SpanDto[]
}

export interface CodeContent {
  code: string
  language: string
}

export type DividerContent = Record<string, never>

export interface TableContent {
  alignments: TableAlignment[]
  header: SpanDto[][] | null
  rows: SpanDto[][][]
}

export interface ImageContent {
  url: string
  alt: string | null
  isDecorative: boolean
  caption: string | null
}

export interface AudioContent {
  url: string
  mimeType: string
  duration: number | null
}
