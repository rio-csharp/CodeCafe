import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { logout } from '@/entities/session'
import { buttonClass } from '@/shared/ui'

export interface UserMenuProps {
  displayName: string
}

/**
 * The account menu behind the display name. Closes on outside click, Escape, or
 * after picking an item — there is deliberately no routing here yet (profile
 * pages are a later milestone), so the menu holds only the way out.
 */
export function UserMenu({ displayName }: UserMenuProps) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!open) {
      return
    }

    const onPointerDown = (event: PointerEvent) => {
      if (rootRef.current !== null && !rootRef.current.contains(event.target as Node)) {
        setOpen(false)
      }
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false)
      }
    }

    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => {
          setOpen((value) => !value)
        }}
        className={buttonClass('ghost')}
      >
        {displayName}
        <svg
          className={['h-3.5 w-3.5 transition-transform', open ? 'rotate-180' : ''].join(' ')}
          viewBox="0 0 24 24"
          fill="none"
          aria-hidden="true"
        >
          <path
            d="m6 9 6 6 6-6"
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </button>

      {open ? (
        <div
          role="menu"
          aria-label={t('header.accountMenu')}
          className="absolute right-0 z-20 mt-2 min-w-40 rounded-xl border border-line bg-card p-1 shadow-lg"
        >
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false)
              logout()
            }}
            className="w-full rounded-lg px-3 py-2 text-left text-sm text-ink transition-colors hover:bg-canvas focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
          >
            {t('header.logout')}
          </button>
        </div>
      ) : null}
    </div>
  )
}
