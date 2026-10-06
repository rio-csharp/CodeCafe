import type { CodeContent } from '../model/types'

/** No syntax highlighting in M4 — just a monospace card that scrolls sideways. */
export function CodeBlock({ content }: { content: CodeContent }) {
  return (
    <pre className="overflow-x-auto rounded-xl border border-line bg-muted-soft p-4 text-sm">
      <code className="font-mono text-ink">{content.code}</code>
    </pre>
  )
}
