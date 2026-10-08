import hljs from 'highlight.js/lib/common'

/** The languages the bundled hljs build can highlight (the "common" set). */
export const HIGHLIGHT_LANGUAGES: readonly string[] = hljs.listLanguages()

/**
 * Highlights `code` and returns HTML (escaped, hljs-token-classed). Unknown
 * or empty languages degrade to escaped plain text; the token classes map
 * onto the app's semantic palette in app/styles/index.css.
 */
export function highlightCode(code: string, language: string): string {
  const lang = language.trim().toLowerCase()
  if (lang !== '' && hljs.getLanguage(lang) !== undefined) {
    try {
      return hljs.highlight(code, { language: lang }).value
    } catch {
      // A grammar can throw on pathological input; fall back to plain text.
    }
  }
  return escapeHtml(code)
}

/** True when the language tag (alias or canonical name) has a grammar. */
export function isHighlightableLanguage(language: string): boolean {
  const lang = language.trim().toLowerCase()
  return lang !== '' && hljs.getLanguage(lang) !== undefined
}

function escapeHtml(value: string): string {
  return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
}
