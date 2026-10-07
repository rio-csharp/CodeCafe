import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { MarkDto } from '@/entities/block'
import type { SimpleMarkKind } from '../lib/marks'

export interface MarkToolbarProps {
  /** Position relative to the block element (the wrapper is `relative`). */
  top: number
  left: number
  /** Mark kinds active across the whole selection. */
  active: ReadonlySet<MarkDto['kind']>
  linkOpen: boolean
  /** Existing href on the selection, prefilled when the link box opens. */
  initialHref: string | null
  onToggle: (kind: SimpleMarkKind) => void
  /** Clicked the link button; the parent decides whether that opens the input. */
  onLinkClick: () => void
  onLinkSubmit: (href: string) => void
  onLinkRemove: () => void
  onLinkCancel: () => void
}

const BUTTONS: { kind: SimpleMarkKind; label: string; className: string; shortcut?: string }[] = [
  { kind: 'bold', label: 'B', className: 'font-bold', shortcut: 'B' },
  { kind: 'italic', label: 'I', className: 'italic', shortcut: 'I' },
  { kind: 'underline', label: 'U', className: 'underline', shortcut: 'U' },
  { kind: 'strike', label: 'S', className: 'line-through' },
  { kind: 'code', label: '<>', className: 'font-mono text-[10px]' },
]

/** ⌘ on macOS, Ctrl+ elsewhere — shown in the hover tooltip. */
const MODIFIER =
  typeof navigator !== 'undefined' && /mac|iphone|ipad/i.test(navigator.platform)
    ? '⌘'
    : 'Ctrl+'

/** The link-editing form; remounts per href so the input starts prefilled. */
function LinkForm({
  initialHref,
  onSubmit,
  onRemove,
  onCancel,
}: {
  initialHref: string | null
  onSubmit: (href: string) => void
  onRemove: () => void
  onCancel: () => void
}) {
  const { t } = useTranslation()
  const [href, setHref] = useState(initialHref ?? '')

  return (
    <form
      className="flex items-center gap-1"
      onSubmit={(event) => {
        event.preventDefault()
        onSubmit(href.trim())
      }}
    >
      <input
        autoFocus
        value={href}
        aria-label={t('editor.linkPlaceholder')}
        placeholder={t('editor.linkPlaceholder')}
        onChange={(event) => {
          setHref(event.target.value)
        }}
        onKeyDown={(event) => {
          if (event.key === 'Escape') {
            onCancel()
          }
        }}
        className="w-44 bg-transparent px-1.5 text-xs text-canvas outline-none placeholder:text-canvas/50"
      />
      {initialHref !== null ? (
        <button
          type="button"
          onClick={onRemove}
          className="rounded-full px-1.5 py-0.5 text-xs font-medium text-canvas/60 transition-colors hover:bg-canvas/10 hover:text-canvas"
        >
          {t('editor.removeLink')}
        </button>
      ) : null}
      <button
        type="submit"
        className="rounded-full px-1.5 py-0.5 text-xs font-medium text-canvas/80 transition-colors hover:bg-canvas/10 hover:text-canvas"
      >
        {t('editor.applyLink')}
      </button>
    </form>
  )
}

/** The floating pill that appears over a text selection inside a block. */
export function MarkToolbar({
  top,
  left,
  active,
  linkOpen,
  initialHref,
  onToggle,
  onLinkClick,
  onLinkSubmit,
  onLinkRemove,
  onLinkCancel,
}: MarkToolbarProps) {
  const { t } = useTranslation()

  return (
    <div
      role="toolbar"
      aria-label={t('editor.formatSelection')}
      style={{ top, left }}
      className="absolute z-20 flex -translate-x-1/2 -translate-y-full items-center gap-0.5 rounded-full bg-ink px-1.5 py-1 text-canvas shadow-lg"
      // Keep the text selection alive while interacting with the toolbar.
      onMouseDown={(event) => {
        event.preventDefault()
      }}
    >
      {linkOpen ? (
        <LinkForm
          key={initialHref ?? ''}
          initialHref={initialHref}
          onSubmit={onLinkSubmit}
          onRemove={onLinkRemove}
          onCancel={onLinkCancel}
        />
      ) : (
        <>
          {BUTTONS.map((button) => {
            const label = t(`editor.marks.${button.kind}`)
            return (
              <button
                key={button.kind}
                type="button"
                aria-label={label}
                title={
                  button.shortcut === undefined ? label : `${label} (${MODIFIER}${button.shortcut})`
                }
                aria-pressed={active.has(button.kind)}
                onClick={() => {
                  onToggle(button.kind)
                }}
                className={`flex h-6 w-6 items-center justify-center rounded-full text-xs transition-colors hover:bg-canvas/10 ${button.className} ${
                  active.has(button.kind) ? 'bg-canvas/15 text-canvas' : 'text-canvas/70'
                }`}
              >
                {button.label}
              </button>
            )
          })}
          <button
            type="button"
            aria-label={t('editor.marks.link')}
            aria-pressed={active.has('link')}
            onClick={onLinkClick}
            className={`flex h-6 w-6 items-center justify-center rounded-full transition-colors hover:bg-canvas/10 ${
              active.has('link') ? 'bg-canvas/15 text-canvas' : 'text-canvas/70'
            }`}
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-3.5 w-3.5">
              <path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71" strokeLinecap="round" strokeLinejoin="round" />
              <path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </button>
        </>
      )}
    </div>
  )
}
