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

  const dialogRef = useRef<HTMLDivElement>(null)
  const closeRef = useRef(onClose)
  useEffect(() => { closeRef.current = onClose }, [onClose])

  useEffect(() => {
    const dialog = dialogRef.current
    if (dialog === null) return
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const focusable = () => Array.from(dialog.querySelectorAll<HTMLElement>(
      'button, a[href], input:not([type="hidden"]), select, textarea, audio[controls], video[controls], [contenteditable="true"], [tabindex]',
    )).filter((element) => !element.matches('[disabled], [tabindex="-1"]') && !element.closest('[hidden], [inert]') && getComputedStyle(element).display !== 'none' && getComputedStyle(element).visibility !== 'hidden')
    ;(focusable()[0] ?? dialog).focus()

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        event.preventDefault()
        closeRef.current()
      }
      if (event.key === 'Tab') {
        const elements = focusable()
        const first = elements[0]
        const last = elements.at(-1)
        if (first === undefined || last === undefined) {
          event.preventDefault()
          dialog.focus()
        } else if (event.shiftKey && (document.activeElement === first || !dialog.contains(document.activeElement))) {
          event.preventDefault()
          last.focus()
        } else if (!event.shiftKey && (document.activeElement === last || !dialog.contains(document.activeElement))) {
          event.preventDefault()
          first.focus()
        }
      }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      document.body.style.overflow = previousOverflow
      if (opener?.isConnected) opener.focus()
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
        ref={dialogRef}
        tabIndex={-1}
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={`relative my-8 w-full rounded-2xl border border-line bg-card shadow-2xl ${
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
