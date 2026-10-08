import type { SlashTarget } from './blockTypes'

/**
 * Markdown-style input rules. When the text before the caret is EXACTLY a
 * marker and the user presses the trigger key (Space for line markers, the
 * third backtick for code), the block converts — Notion-style.
 *
 * Deliberately absent: `- [ ]` — the `- ` bullet rule fires on the first
 * space, long before the brackets can be typed. `[]` / `[x]` is the to-do
 * marker instead.
 */
const SPACE_RULES: readonly { marker: RegExp; target: (match: RegExpMatchArray) => SlashTarget }[] = [
  { marker: /^(#{1,3})$/, target: (m) => ({ type: 'heading', level: m[1]!.length as 1 | 2 | 3 }) },
  { marker: /^>$/, target: () => ({ type: 'quote' }) },
  { marker: /^[-*]$/, target: () => ({ type: 'bulleted-list' }) },
  { marker: /^\d+[.)]$/, target: () => ({ type: 'numbered-list' }) },
  { marker: /^\[\]$/, target: () => ({ type: 'todo' }) },
  { marker: /^\[[xX]\]$/, target: () => ({ type: 'todo', checked: true }) },
  { marker: /^---$/, target: () => ({ type: 'divider' }) },
]

/** Space pressed: convert when the whole pre-caret text is a marker. */
export function matchSpaceRule(beforeCaret: string): SlashTarget | null {
  for (const rule of SPACE_RULES) {
    const match = rule.marker.exec(beforeCaret)
    if (match !== null) {
      return rule.target(match)
    }
  }
  return null
}

/** Third backtick pressed with nothing but `` `` `` before the caret: a code block. */
export function matchBacktickRule(beforeCaret: string): SlashTarget | null {
  return beforeCaret === '``' ? { type: 'code' } : null
}

/** Third dash pressed with nothing but `--` before the caret: a divider. */
export function matchDashRule(beforeCaret: string): SlashTarget | null {
  return beforeCaret === '--' ? { type: 'divider' } : null
}
