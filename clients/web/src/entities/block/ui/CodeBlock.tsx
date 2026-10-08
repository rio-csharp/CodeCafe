import { highlightCode } from '../lib/highlight'
import type { CodeContent } from '../model/types'

/** A monospace card with syntax highlighting that scrolls sideways. */
export function CodeBlock({ content }: { content: CodeContent }) {
  return (
    <pre className="overflow-x-auto rounded-xl border border-line bg-muted-soft p-4 text-sm">
      <code
        className="hljs font-mono text-ink"
        // highlightCode escapes everything it does not wrap in token spans.
        dangerouslySetInnerHTML={{ __html: highlightCode(content.code, content.language) }}
      />
    </pre>
  )
}
