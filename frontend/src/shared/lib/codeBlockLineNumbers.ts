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
    const code = pre.querySelector('code')
    if (!code) return
    const lineCount = code.textContent?.split('\n').length || 1

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
