import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'

export interface OverflowMenuItem {
  id: string
  label: string
  /** Path children for the 16×16 icon frame, like the chrome pills take. */
  icon?: ReactNode
  onSelect: () => void
  /** Tints the row as destructive — delete and friends. */
  danger?: boolean
  disabled?: boolean
  /** Marks the row as the current choice, e.g. a display preference. */
  selected?: boolean
  /** Keeps the menu open after the click, for actions whose label confirms itself. */
  keepOpen?: boolean
}

export interface OverflowMenuProps {
  /** Names both the trigger and the popup it opens. */
  label: string
  items: readonly OverflowMenuItem[]
  /** Shorter text for the pill, when the label is too wordy to sit in a bar. */
  triggerText?: string
  /** `pill` matches the reader chrome; `icon` is the bare round button. */
  trigger?: 'pill' | 'icon'
  align?: 'start' | 'end'
}

/**
 * The ⋯ behind which secondary actions hide so a header can keep its width for
 * the thing being read. Closes on outside click, Escape, or picking an item.
 */
export function OverflowMenu({
  label,
  items,
  triggerText,
  trigger = 'pill',
  align = 'end',
}: OverflowMenuProps) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) {
      return
    }
    // A menu is a keyboard surface: the first item takes focus on open, and
    // Escape hands it back to the trigger.
    const menuItems = () =>
      Array.from(
        menuRef.current?.querySelectorAll<HTMLButtonElement>(
          '[role="menuitem"], [role="menuitemcheckbox"]',
        ) ?? [],
      ).filter((item) => !item.disabled)
    menuItems()[0]?.focus()

    const onPointerDown = (event: PointerEvent) => {
      if (rootRef.current !== null && !rootRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
        triggerRef.current?.focus()
        return
      }

      if (!menuRef.current?.contains(event.target as Node)) {
        return
      }

      if (event.key === 'Tab') {
        setOpen(false)
        return
      }

      const availableItems = menuItems()
      const currentIndex = availableItems.indexOf(document.activeElement as HTMLButtonElement)
      if (availableItems.length === 0 || currentIndex < 0) {
        return
      }

      let nextIndex: number | null = null
      if (event.key === 'ArrowDown') {
        nextIndex = (currentIndex + 1) % availableItems.length
      } else if (event.key === 'ArrowUp') {
        nextIndex = (currentIndex - 1 + availableItems.length) % availableItems.length
      } else if (event.key === 'Home') {
        nextIndex = 0
      } else if (event.key === 'End') {
        nextIndex = availableItems.length - 1
      }

      if (nextIndex !== null) {
        event.preventDefault()
        availableItems[nextIndex]?.focus()
      }
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  const triggerClass =
    trigger === 'pill'
      ? [
          'inline-flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-xs font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent',
          open
            ? 'border-accent/40 bg-accent-soft text-accent-strong'
            : 'border-line text-muted hover:bg-muted-soft hover:text-ink',
        ].join(' ')
      : 'grid size-8 shrink-0 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent'

  return (
    <div ref={rootRef} className="relative shrink-0">
      <button
        ref={triggerRef}
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={label}
        title={label}
        onClick={() => {
          setOpen((value) => !value)
        }}
        className={triggerClass}
      >
        <svg viewBox="0 0 16 16" className="size-3.5 shrink-0" fill="currentColor" aria-hidden="true">
          <circle cx="3" cy="8" r="1.3" />
          <circle cx="8" cy="8" r="1.3" />
          <circle cx="13" cy="8" r="1.3" />
        </svg>
        {trigger === 'pill' ? <span className="hidden sm:inline">{triggerText ?? label}</span> : null}
      </button>

      {open ? (
        <div
          ref={menuRef}
          role="menu"
          aria-label={label}
          className={`absolute z-20 mt-1 min-w-40 max-w-[min(16rem,90vw)] rounded-xl border border-line bg-card p-1 shadow-lg ${
            align === 'end' ? 'right-0' : 'left-0'
          }`}
        >
          {items.map((item) => (
            <button
              key={item.id}
              type="button"
              role={item.selected !== undefined ? 'menuitemcheckbox' : 'menuitem'}
              aria-checked={item.selected}
              tabIndex={item.disabled ? -1 : 0}
              disabled={item.disabled}
              onClick={() => {
                if (item.keepOpen !== true) {
                  setOpen(false)
                  triggerRef.current?.focus()
                }
                item.onSelect()
              }}
              className={[
                'flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50',
                item.danger === true
                  ? 'text-danger hover:bg-danger-soft'
                  : 'text-ink hover:bg-canvas',
              ].join(' ')}
            >
              {item.icon !== undefined ? (
                <svg
                  viewBox="0 0 16 16"
                  className="size-3.5 shrink-0"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.6"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  aria-hidden="true"
                >
                  {item.icon}
                </svg>
              ) : null}
              <span className="flex-1">{item.label}</span>
              {item.selected === true ? (
                <svg
                  viewBox="0 0 16 16"
                  className="size-3.5 shrink-0 text-accent-strong"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  aria-hidden="true"
                >
                  <path d="m3.5 8.5 3 3 6-7" />
                </svg>
              ) : null}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  )
}
