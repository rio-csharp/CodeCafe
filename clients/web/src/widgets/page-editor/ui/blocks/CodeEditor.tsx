import { useTranslation } from 'react-i18next'
import type { CodeContent } from '@/entities/block'
import type { EditorBlock } from '../../lib/draft'

export interface CodeEditorProps {
  block: EditorBlock
  onChange: (content: Record<string, unknown>) => void
}

const INPUT_CLASS =
  'rounded-md bg-transparent px-2 py-1 text-xs text-muted placeholder:text-muted/60 focus:outline focus:outline-2 focus:outline-accent'

/** Code editing: a plain textarea (marks do not apply) plus the language tag. */
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
      <textarea
        value={content.code}
        aria-label={t('editor.code')}
        placeholder={t('editor.codePlaceholder')}
        rows={Math.max(2, content.code.split('\n').length)}
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
        className="w-full resize-none bg-transparent px-3 py-2 font-mono text-[13px] leading-relaxed text-ink placeholder:text-muted/60 focus:outline-none"
      />
    </div>
  )
}
