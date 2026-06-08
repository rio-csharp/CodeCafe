/**
 * Sync `.line-numbers` gutter decorations on every `<pre>` inside `container`.
 *
 * Used by both the TipTap editor (during edit) and the read-only viewer
 * (during render) so the gutter reflects the current number of code lines.
 * No-op when the line count is unchanged since the last invocation.
 */
export function applyCodeBlockLineNumbers(container: HTMLElement | null | undefined): void {
  if (!container) return

  container.querySelectorAll('pre').forEach((pre) => {
    // Remove whitespace-only text nodes so they don't become anonymous
    // flex items when pre is display: flex (creates fake indentation).
    Array.from(pre.childNodes).forEach((node) => {
      if (node.nodeType === Node.TEXT_NODE && !node.textContent?.trim()) {
        pre.removeChild(node)
      }
    })

    const code = pre.querySelector('code')
    if (!code) return
    const raw = code.textContent ?? ''
    const normalized = raw.replace(/\r\n/g, '\n').replace(/\n+$/, '')
    const lineCount = Math.max(1, normalized.split('\n').length)

    let lineNumbers = pre.querySelector('.line-numbers') as HTMLElement | null
    if (!lineNumbers) {
      lineNumbers = document.createElement('div')
      lineNumbers.className = 'line-numbers'
      lineNumbers.setAttribute('aria-hidden', 'true')
      pre.insertBefore(lineNumbers, code)
    }

    const existingSpans = lineNumbers.querySelectorAll('span')
    if (existingSpans.length === lineCount) return

    lineNumbers.innerHTML = ''
    for (let i = 1; i <= lineCount; i++) {
      const span = document.createElement('span')
      span.textContent = String(i)
      lineNumbers.appendChild(span)
    }
  })
}
