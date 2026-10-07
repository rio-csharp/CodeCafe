import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'

export interface ReaderChromeProps {
  title: string
  refreshing: boolean
  onRefresh: () => void
  wide: boolean
  onToggleWide: () => void
  /** Shown only when the caller also passes onEdit. */
  canEdit?: boolean
  onEdit?: () => void
}

/**
 * The page's own action strip, sticky inside the scrolling text column: the
 * title being read plus small labelled pills (refresh, copy link, width).
 * Labels hide on phones, where icons alone carry the meaning.
 */
export function ReaderChrome({ title, refreshing, onRefresh, wide, onToggleWide, canEdit = false, onEdit }: ReaderChromeProps) {
  const { t } = useTranslation()

  return (
    <div className="sticky top-0 z-10 -mx-4 -mt-6 mb-4 flex items-center justify-between gap-3 bg-card/95 px-4 py-2 backdrop-blur-sm sm:-mx-8 sm:px-8 xl:-mx-12 xl:px-12">
      <h1 className="min-w-0 truncate text-lg font-semibold text-ink">{title}</h1>

      <div className="flex shrink-0 items-center gap-2">
        {canEdit && onEdit !== undefined ? (
          <ChromePill label={t('reader.edit')} onClick={onEdit}>
            <path d="M11.7 2.9a1.4 1.4 0 0 1 2 2L6 12.6l-2.8.8.8-2.8Z" />
          </ChromePill>
        ) : null}

        <ChromePill
          label={t('reader.refresh')}
          onClick={onRefresh}
          disabled={refreshing}
          iconClass={refreshing ? 'origin-center animate-spin' : undefined}
        >
          <path d="M13.5 8a5.5 5.5 0 1 1-1.61-3.89M13.5 2.5v2.6h-2.6" />
        </ChromePill>

        <CopyLinkPill />

        <ChromePill
          label={wide ? t('reader.narrowWidth') : t('reader.fullWidth')}
          pressed={wide}
          onClick={onToggleWide}
        >
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
        </ChromePill>
      </div>
    </div>
  )
}

/** Copies the current URL; the pill swaps to a green check briefly. */
function CopyLinkPill() {
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
    <ChromePill
      label={copied ? t('reader.linkCopied') : t('reader.copyLink')}
      onClick={copy}
      className={copied ? 'border-success/40 text-success' : undefined}
    >
      {copied ? (
        <path d="m3.5 8.5 3 3 6-7" />
      ) : (
        <path d="M6.5 9.5 13 3M5 7V3.5A1.5 1.5 0 0 1 6.5 2h4A1.5 1.5 0 0 1 12 3.5V5m-1 5v4a1.5 1.5 0 0 1-1.5 1.5h-4A1.5 1.5 0 0 1 4 14v-4a1.5 1.5 0 0 1 1.5-1.5H7" />
      )}
    </ChromePill>
  )
}

function ChromePill({
  label,
  onClick,
  pressed,
  disabled,
  className,
  iconClass,
  children,
}: {
  label: string
  onClick: () => void
  pressed?: boolean
  disabled?: boolean
  className?: string
  iconClass?: string
  children: ReactNode
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
        'inline-flex items-center gap-1.5 rounded-lg border px-2.5 py-1.5 text-xs font-medium transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent disabled:opacity-50',
        pressed === true
          ? 'border-accent/40 bg-accent-soft text-accent-strong'
          : 'border-line text-muted hover:bg-muted-soft hover:text-ink',
        className ?? '',
      ].join(' ')}
    >
      <svg
        viewBox="0 0 16 16"
        className={`size-3.5 shrink-0 ${iconClass ?? ''}`}
        fill="none"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
        aria-hidden="true"
      >
        {children}
      </svg>
      <span className="hidden sm:inline">{label}</span>
    </button>
  )
}
