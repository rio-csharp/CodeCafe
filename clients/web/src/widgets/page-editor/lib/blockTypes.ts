import type { SpanDto } from '@/entities/block'
import { spansPlainText } from './spans'

/** A slash-menu target: which block type to turn into, plus type-specific extras. */
export type SlashTarget =
  | { type: 'paragraph' }
  | { type: 'heading'; level: 1 | 2 | 3 }
  | { type: 'quote' }
  | { type: 'todo' }
  | { type: 'callout' }
  | { type: 'divider' }
  | { type: 'code' }
  | { type: 'table' }

export interface SlashItem {
  /** Stable id; the i18n label lives at `editor.slash.<id>`. */
  id: string
  target: SlashTarget
  /** Filter terms, both languages — the menu is how people discover blocks. */
  keywords: string[]
}

export const SLASH_ITEMS: readonly SlashItem[] = [
  { id: 'paragraph', target: { type: 'paragraph' }, keywords: ['text', 'paragraph', 'plain', '正文', '段落', '文字'] },
  { id: 'heading1', target: { type: 'heading', level: 1 }, keywords: ['h1', 'heading', 'title', '标题'] },
  { id: 'heading2', target: { type: 'heading', level: 2 }, keywords: ['h2', 'heading', 'subtitle', '标题'] },
  { id: 'heading3', target: { type: 'heading', level: 3 }, keywords: ['h3', 'heading', '标题'] },
  { id: 'quote', target: { type: 'quote' }, keywords: ['quote', 'blockquote', '引用'] },
  { id: 'todo', target: { type: 'todo' }, keywords: ['todo', 'task', 'checkbox', 'check', '待办', '任务'] },
  { id: 'callout', target: { type: 'callout' }, keywords: ['callout', 'info', 'warning', 'note', '提示', '标注'] },
  { id: 'divider', target: { type: 'divider' }, keywords: ['divider', 'hr', 'line', 'rule', '分割', '分隔'] },
  { id: 'code', target: { type: 'code' }, keywords: ['code', 'snippet', '代码', '代码块'] },
  { id: 'table', target: { type: 'table' }, keywords: ['table', 'grid', '表格'] },
]

/** Case-insensitive filter over ids, keywords and (via keywords) both locales. */
export function filterSlashItems(query: string): SlashItem[] {
  const q = query.trim().toLowerCase()
  if (q.length === 0) {
    return [...SLASH_ITEMS]
  }
  return SLASH_ITEMS.filter(
    (item) =>
      item.id.toLowerCase().includes(q) || item.keywords.some((k) => k.toLowerCase().includes(q)),
  )
}

/**
 * A block's text starts a slash query when it begins with "/" and the rest is
 * a single word — spaces end the query, Notion-style.
 */
export function detectSlashQuery(plainText: string): string | null {
  if (!plainText.startsWith('/')) {
    return null
  }
  const rest = plainText.slice(1)
  return /\s/.test(rest) ? null : rest
}

/** Fresh content for a conversion target, carrying over the stripped text. */
export function contentForTarget(target: SlashTarget, spans: SpanDto[]): Record<string, unknown> {
  switch (target.type) {
    case 'heading':
      return { level: target.level, spans }
    case 'todo':
      return { checked: false, spans }
    case 'callout':
      return { variant: 'info', spans }
    case 'divider':
      return {}
    case 'code':
      // The stripped text becomes the snippet; the language tag starts generic.
      return { code: spansPlainText(spans), language: 'text' }
    case 'table':
      return {
        alignments: ['none', 'none'],
        header: null,
        rows: [
          [[], []],
          [[], []],
        ],
      }
    default:
      return { spans }
  }
}
