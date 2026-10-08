import { useTranslation } from 'react-i18next'
import { highlightCode } from '@/entities/block'
import type { CodeContent } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'

export interface CodeEditorProps {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
}

const INPUT_CLASS =
  'rounded-md bg-transparent px-2 py-1 text-xs text-muted placeholder:text-muted/60 focus:outline focus:outline-2 focus:outline-accent'

// Both layers share the exact same typography so the transparent textarea
// overlays the highlighted code pixel-perfectly.
const CODE_TYPOGRAPHY = 'px-3 py-2 font-mono text-[13px] leading-relaxed whitespace-pre-wrap break-words'

/**
 * Code editing: a transparent textarea stacked over the highlighted render —
 * the pre sizes the stack, the textarea captures typing and the caret.
 */
export function CodeEditor({ block, onChange }: CodeEditorProps) {
  const { t } = useTranslation()
  const content = block.content as CodeContent

  return (
    <div className="rounded-xl bg-muted-soft/60">
      <input
        value={content.language}
        aria-label={t('editor.codeLanguage')}
        placeholder={t('editor.codeLanguage')}
        onChange={(event) => {
          onChange({ ...content, language: event.target.value })
        }}
        className={`${INPUT_CLASS} ml-2 mt-1`}
      />
      <div className="relative">
        <pre aria-hidden className={`hljs text-ink ${CODE_TYPOGRAPHY}`}>
          <code
            // The trailing newline keeps the pre as tall as the textarea's
            // implicit last line.
            dangerouslySetInnerHTML={{
              __html: highlightCode(`${content.code}\n`, content.language),
            }}
          />
        </pre>
        <textarea
          value={content.code}
          aria-label={t('editor.code')}
          placeholder={t('editor.codePlaceholder')}
          onChange={(event) => {
            onChange({ ...content, code: event.target.value })
          }}
          onKeyDown={(event) => {
            // Tab indents the code itself, not the block.
            if (event.key === 'Tab') {
              event.preventDefault()
              const area = event.currentTarget
              const { selectionStart, selectionEnd } = area
              const next = `${content.code.slice(0, selectionStart)}  ${content.code.slice(selectionEnd)}`
              onChange({ ...content, code: next })
              requestAnimationFrame(() => {
                area.setSelectionRange(selectionStart + 2, selectionStart + 2)
              })
            }
          }}
          className={`absolute inset-0 h-full w-full resize-none overflow-hidden bg-transparent text-transparent caret-accent placeholder:text-muted/60 focus:outline-none ${CODE_TYPOGRAPHY}`}
        />
      </div>
    </div>
  )
}
