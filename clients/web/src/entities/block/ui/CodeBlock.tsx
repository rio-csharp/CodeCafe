import { highlightCode } from '../lib/highlight'
import type { CodeContent } from '../model/types'

/** A monospace card with syntax highlighting that scrolls sideways. */
export function CodeBlock({ content }: { content: CodeContent }) {
  const language = content.language.trim()
  return (
    <div className="relative">
      {language !== '' ? (
        // Outside the scrolling pre, so the label stays put on wide code.
        <span className="absolute top-2 right-3 font-mono text-[10px] text-muted select-none">
          {language}
        </span>
      ) : null}
      <pre className="overflow-x-auto rounded-xl border border-line bg-muted-soft p-4 text-sm">
        <code
          className="hljs font-mono text-ink"
          // highlightCode escapes everything it does not wrap in token spans.
          dangerouslySetInnerHTML={{ __html: highlightCode(content.code, content.language) }}
        />
      </pre>
    </div>
  )
}
