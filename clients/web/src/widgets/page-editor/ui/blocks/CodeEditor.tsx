import { useId, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { highlightCode, HIGHLIGHT_LANGUAGES } from '@/entities/block'
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
      <LanguageCombobox
        value={content.language}
        onPick={(language) => {
          onChange({ ...content, language })
        }}
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

/**
 * The language field: free text with a themed suggestion dropdown. A native
 * <datalist> would be drawn by the OS and ignore the app's palette, so the
 * listbox is hand-rolled — arrows move, Enter picks, Escape closes, typing
 * filters, and anything unlisted still works.
 */
function LanguageCombobox({
  value,
  onPick,
}: {
  value: string
  onPick: (language: string) => void
}) {
  const { t } = useTranslation()
  const listboxId = useId()
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(0)

  const query = value.trim().toLowerCase()
  const options = HIGHLIGHT_LANGUAGES.filter((language) => language.includes(query)).slice(0, 8)

  const pick = (language: string) => {
    onPick(language)
    setOpen(false)
  }

  return (
    <div className="relative ml-2 mt-1 self-start">
      <input
        value={value}
        role="combobox"
        aria-expanded={open && options.length > 0}
        aria-controls={listboxId}
        aria-label={t('editor.codeLanguage')}
        placeholder={t('editor.codeLanguage')}
        onChange={(event) => {
          onPick(event.target.value)
          setOpen(true)
          setActiveIndex(0)
        }}
        onFocus={() => {
          setOpen(true)
          setActiveIndex(0)
        }}
        onBlur={() => {
          // mousedown on an option beats blur, so clicks still register.
          setOpen(false)
        }}
        onKeyDown={(event) => {
          // ArrowDown opens a closed list, like a native select.
          if (!open) {
            if (event.key === 'ArrowDown') {
              setOpen(true)
              setActiveIndex(0)
            }
            return
          }
          if (options.length === 0) {
            return
          }
          if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault()
            const delta = event.key === 'ArrowDown' ? 1 : -1
            setActiveIndex((current) => (current + delta + options.length) % options.length)
          } else if (event.key === 'Enter') {
            event.preventDefault()
            pick(options[activeIndex] ?? options[0]!)
          } else if (event.key === 'Escape') {
            // Close the list, not the editor: keep the event from bubbling up.
            event.stopPropagation()
            setOpen(false)
          }
        }}
        className={INPUT_CLASS}
      />
      {open && options.length > 0 ? (
        <ul
          id={listboxId}
          role="listbox"
          aria-label={t('editor.codeLanguage')}
          className="absolute left-0 z-20 mt-1 max-h-48 min-w-40 overflow-y-auto rounded-lg border border-line bg-card py-1 shadow-lg"
        >
          {options.map((language, index) => (
            <li
              key={language}
              role="option"
              aria-selected={index === activeIndex}
              onMouseDown={(event) => {
                event.preventDefault()
                pick(language)
              }}
              onMouseEnter={() => {
                setActiveIndex(index)
              }}
              className={`cursor-pointer px-3 py-1 font-mono text-xs ${
                index === activeIndex ? 'bg-accent-soft text-accent-strong' : 'text-ink'
              }`}
            >
              {language}
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  )
}
