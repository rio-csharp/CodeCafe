import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

export interface ReaderChromeProps {
  title: string
  refreshing: boolean
  onRefresh: () => void
  wide: boolean
  onToggleWide: () => void
}

/**
 * The page's own action strip, sticky inside the scrolling text column: the
 * title being read, refresh, copy-link, and the width toggle. It lives in the
 * flow rather than the top bar because everything on it is about this page.
 */
export function ReaderChrome({ title, refreshing, onRefresh, wide, onToggleWide }: ReaderChromeProps) {
  const { t } = useTranslation()

  return (
    <div className="sticky top-0 z-10 -mx-4 -mt-6 mb-4 flex items-center justify-between gap-4 border-b border-line/60 bg-canvas/95 px-4 py-2 backdrop-blur-sm sm:-mx-8 sm:px-8 xl:-mx-12 xl:px-12">
      <h1 className="min-w-0 truncate text-lg font-semibold text-ink">{title}</h1>

      <div className="flex shrink-0 items-center gap-1">
        <ChromeButton label={t('reader.refresh')} onClick={onRefresh} disabled={refreshing}>
          <path
            d="M13.5 8a5.5 5.5 0 1 1-1.61-3.89M13.5 2.5v2.6h-2.6"
            className={refreshing ? 'origin-center animate-spin' : undefined}
          />
        </ChromeButton>

        <CopyLinkButton />

        <ChromeButton label={t('reader.fullWidth')} pressed={wide} onClick={onToggleWide}>
          {wide ? (
            <>
              <path d="M3 9.5h4v4" />
              <path d="M13 6.5h-4v-4" />
              <path d="M9.5 6.5 14 2" />
              <path d="M6.5 9.5 2 14" />
            </>
          ) : (
            <>
              <path d="M10 2h4v4" />
              <path d="M6 14H2v-4" />
              <path d="M14 2 9.5 6.5" />
              <path d="M2 14l4.5-4.5" />
            </>
          )}
        </ChromeButton>
      </div>
    </div>
  )
}

/** Copies the current URL; swaps to a check briefly instead of a toast. */
function CopyLinkButton() {
  const { t } = useTranslation()
  const [copied, setCopied] = useState(false)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    return () => {
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current)
      }
    }
  }, [])

  const copy = () => {
    void navigator.clipboard.writeText(window.location.href).then(() => {
      setCopied(true)
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current)
      }
      timerRef.current = setTimeout(() => {
        setCopied(false)
      }, 1500)
    })
  }

  return (
    <ChromeButton
      label={copied ? t('reader.linkCopied') : t('reader.copyLink')}
      onClick={copy}
      className={copied ? 'text-success' : undefined}
    >
      {copied ? <path d="m3.5 8.5 3 3 6-7" /> : <path d="M6.5 9.5 13 3M5 7V3.5A1.5 1.5 0 0 1 6.5 2h4A1.5 1.5 0 0 1 12 3.5V5m-1 5v4a1.5 1.5 0 0 1-1.5 1.5h-4A1.5 1.5 0 0 1 4 14v-4a1.5 1.5 0 0 1 1.5-1.5H7" />}
    </ChromeButton>
  )
}

function ChromeButton({
  label,
  onClick,
  pressed,
  disabled,
  className,
  children,
}: {
  label: string
  onClick: () => void
  pressed?: boolean
  disabled?: boolean
  className?: string
  children: React.ReactNode
}) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      aria-pressed={pressed}
      disabled={disabled}
      onClick={onClick}
      className={[
        'grid size-8 place-items-center rounded-full transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent disabled:opacity-50',
        pressed === true ? 'bg-accent-soft text-accent-strong' : 'text-muted hover:bg-muted-soft hover:text-ink',
        className ?? '',
      ].join(' ')}
    >
      <svg
        viewBox="0 0 16 16"
        className="size-4"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
      >
        {children}
      </svg>
    </button>
  )
}
