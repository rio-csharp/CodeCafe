// Shared building blocks for the code-block copy affordance, used by both the
// React portal component (CodeBlockCopyButton) and TipTapViewer's
// event-delegated implementation. The SVG strings mirror the lucide-react
// `Copy`/`Check` glyphs so both implementations render identical icons.

export const CODE_COPY_ICON = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>`

export const CODE_CHECK_ICON = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>`

/** How long the "copied" acknowledgement is shown before reverting. */
export const CODE_COPY_FEEDBACK_MS = 2000

/**
 * Copy the text of the `<code>` inside a `<pre>`.
 * Resolves `false` when the block has no code element; rejects on clipboard
 * failure (callers typically swallow that).
 */
export async function copyCodeFromPre(pre: HTMLElement): Promise<boolean> {
  const code = pre.querySelector('code')
  if (!code) return false
  await navigator.clipboard.writeText(code.textContent ?? '')
  return true
}
