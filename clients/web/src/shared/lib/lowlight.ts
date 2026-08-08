import { createLowlight, common } from 'lowlight'
import type { Nodes } from 'hast'

/**
 * Shared lowlight instance for TipTap's CodeBlockLowlight extension.
 *
 * The TipTap extension stores the configured `lowlight` instance internally
 * and re-uses it across renders; creating a fresh `createLowlight(common)` per
 * consumer meant two parallel highlighters were always loaded. This module
 * exports a single instance that both the editor and viewer widgets import.
 */
export const lowlight = createLowlight(common)

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function hastNodeToHtml(node: Nodes): string {
  if (node.type === 'text') {
    return escapeHtml(node.value)
  }
  if (node.type === 'element') {
    const props = node.properties || {}
    const className = Array.isArray(props.className)
      ? props.className.join(' ')
      : props.className
    const attrs = className ? ` class="${className}"` : ''
    const children = node.children.map(hastNodeToHtml).join('')
    return `<${node.tagName}${attrs}>${children}</${node.tagName}>`
  }
  if (node.type === 'root') {
    return node.children.map(hastNodeToHtml).join('')
  }
  return ''
}

/**
 * Highlight all `<pre><code>` blocks inside a container using lowlight.
 * Mutates the DOM in-place.
 */
export function highlightCodeBlocks(container: HTMLElement): void {
  container.querySelectorAll('pre code').forEach((code) => {
    const languageMatch = code.className.match(/language-(\w+)/)
    const language = languageMatch?.[1] || 'plaintext'
    const text = (code.textContent || '').replace(/\r\n/g, '\n').replace(/\n+$/, '')

    // The `hljs` class is added to `code` itself below, so check the class
    // list (a descendant query would never match and is dead code).
    if (code.classList.contains('hljs')) return

    try {
      const result = lowlight.highlight(language, text)
      code.innerHTML = hastNodeToHtml(result)
      code.classList.add('hljs')
    } catch {
      // Language not registered — silently skip
    }
  })
}
