import { useEffect, useRef } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

export interface DialogShellProps {
  title: string
  onClose: () => void
  /** Defaults to a comfortable form width. */
  wide?: boolean
  children: ReactNode
}

/**
 * The one modal frame every dialog shares: dimmed blurred backdrop, a card
 * panel with a titled header, Escape and backdrop-click to close. Bodies fill
 * in their own sections.
 */
export function DialogShell({ title, onClose, wide = false, children }: DialogShellProps) {
  const { t } = useTranslation()
  const panelRef = useRef<HTMLDivElement>(null)
  const restoreFocusRef = useRef<HTMLElement | null>(null)
  // A fresh arrow-function prop per render must not re-run the focus setup.
  const onCloseRef = useRef(onClose)
  useEffect(() => {
    onCloseRef.current = onClose
  }, [onClose])

  useEffect(() => {
    restoreFocusRef.current = document.activeElement as HTMLElement | null
    const panel = panelRef.current
    const focusables = (): HTMLElement[] =>
      Array.from(
        panel?.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])',
        ) ?? [],
      )

    // The panel itself takes focus when it has no controls, so Escape and Tab still work.
    const firstFocusable = focusables()[0]
    if (firstFocusable !== undefined) {
      firstFocusable.focus()
    } else {
      panel?.focus()
    }

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        // Keep a page-level Escape handler (the editor's cancel) from also firing.
        event.stopPropagation()
        onCloseRef.current()
        return
      }

      if (event.key !== 'Tab' || panel === null) {
        return
      }

      const items = focusables()
      if (items.length === 0) {
        event.preventDefault()
        panel.focus()
        return
      }

      const first = items[0]!
      const last = items[items.length - 1]!
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      restoreFocusRef.current?.focus()
    }
  }, [])

  return (
    <div className="fixed inset-0 z-50 grid place-items-center overflow-y-auto p-4">
      <div
        aria-hidden="true"
        className="absolute inset-0 bg-canvas/70 backdrop-blur-sm"
        onClick={onClose}
      />

      <div
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        tabIndex={-1}
        className={`relative my-8 w-full rounded-2xl border border-line bg-card shadow-2xl focus:outline-none ${
          wide ? 'max-w-xl' : 'max-w-md'
        }`}
      >
        <div className="flex items-center justify-between border-b border-line px-6 py-4">
          <h2 className="text-base font-semibold text-ink">{title}</h2>
          <button
            type="button"
            aria-label={t('dialog.close')}
            onClick={onClose}
            className="grid size-8 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
          >
            <svg
              viewBox="0 0 16 16"
              className="size-4"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.6"
              strokeLinecap="round"
              aria-hidden="true"
            >
              <path d="m4 4 8 8M12 4l-8 8" />
            </svg>
          </button>
        </div>

        <div className="px-6 py-5">{children}</div>
      </div>
    </div>
  )
}

/** A labeled form field: visible label above, control below. */
export function DialogField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-xs font-medium text-muted">{label}</span>
      {children}
    </label>
  )
}
