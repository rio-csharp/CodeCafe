import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { OverflowMenu } from '@/shared/ui'
import type { OverflowMenuItem } from '@/shared/ui'

/** Breathing room between the sticky bar's bottom edge and a jumped-to heading. */
const CHROME_GAP_PX = 16

export interface ReaderChromeProps {
  title: string
  refreshing: boolean
  onRefresh: () => void
  wide: boolean
  onToggleWide: () => void
  /** Shown only when the caller also passes onEdit. */
  canEdit?: boolean
  onEdit?: () => void
  /** The page-favorite toggle, supplied by the page; hidden while undefined. */
  favoriteAction?: ReactNode
  /** Markdown export of the open page; anonymous readers may use it too. */
  onExportPage?: () => void
  /** Page-level sharing dialog; writers only, so undefined hides the pill. */
  onSharePage?: () => void
}

/**
 * The page's own action strip, sticky inside the scrolling text column: the
 * title being read, then the few pills worth a permanent seat. Refresh, copy
 * link, export, share and the width preference fold into the ⋯ so the title
 * keeps the bar to itself; the writer's Edit stays out as the primary action.
 */
export function ReaderChrome({ title, refreshing, onRefresh, wide, onToggleWide, canEdit = false, onEdit, favoriteAction, onExportPage, onSharePage }: ReaderChromeProps) {
  const { t } = useTranslation()
  const copyLink = useCopyLink()
  const barRef = useChromeOffset()

  const actions: OverflowMenuItem[] = [
    {
      id: 'refresh',
      label: t('reader.refresh'),
      icon: <path d="M13.5 8a5.5 5.5 0 1 1-1.61-3.89M13.5 2.5v2.6h-2.6" />,
      onSelect: onRefresh,
      disabled: refreshing,
    },
    ...(onExportPage !== undefined
      ? [
          {
            id: 'export',
            label: t('reader.exportPage'),
            icon: <path d="M8 2.5v7M5.5 7 8 9.5 10.5 7M3 12.5h10" />,
            onSelect: onExportPage,
          },
        ]
      : []),
    ...(onSharePage !== undefined
      ? [
          {
            id: 'share',
            label: t('reader.sharePage'),
            icon: (
              <path d="M10.5 13.5v-1a2.5 2.5 0 0 0-2.5-2.5H5a2.5 2.5 0 0 0-2.5 2.5v1M6.5 7.5a2 2 0 1 0 0-4 2 2 0 0 0 0 4ZM13.5 13.5v-1a2.5 2.5 0 0 0-1.25-2.16M10.75 3.6a2 2 0 1 1 0 3.8" />
            ),
            onSelect: onSharePage,
          },
        ]
      : []),
    // Copying confirms itself by label, so the row keeps the menu open.
    { id: 'copy', label: copyLink.label, icon: copyLink.icon, onSelect: copyLink.copy, keepOpen: true },
    {
      id: 'width',
      label: wide ? t('reader.narrowWidth') : t('reader.fullWidth'),
      selected: wide,
      onSelect: onToggleWide,
    },
  ]

  return (
    <div ref={barRef} className="sticky top-0 z-10 -mx-4 -mt-6 mb-4 flex items-center justify-between gap-3 bg-card/95 px-4 py-2 backdrop-blur-sm sm:-mx-8 sm:px-8 xl:-mx-12 xl:px-12">
      <h1 className="min-w-0 truncate text-lg font-semibold text-ink">{title}</h1>

      <div className="flex shrink-0 items-center gap-2">
        {favoriteAction}

        {canEdit && onEdit !== undefined ? (
          <ChromePill label={t('reader.edit')} onClick={onEdit}>
            <path d="M11.7 2.9a1.4 1.4 0 0 1 2 2L6 12.6l-2.8.8.8-2.8Z" />
          </ChromePill>
        ) : null}

        <OverflowMenu
          label={t('reader.moreActions')}
          triggerText={t('reader.more')}
          items={actions}
        />
      </div>
    </div>
  )
}

/**
 * Publishes this bar's height as `--reader-chrome-offset` while it is on screen.
 * A fragment jump scrolls its target to the scrollport's top edge, which is
 * exactly where the sticky bar sits, so headings read the variable back as
 * their scroll margin (see `HeadingBlock`).
 */
function useChromeOffset() {
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const bar = ref.current
    if (bar === null) {
      return
    }
    const root = document.documentElement
    const publish = () => {
      const height = bar.getBoundingClientRect().height
      root.style.setProperty('--reader-chrome-offset', `${Math.round(height) + CHROME_GAP_PX}px`)
    }
    publish()
    // jsdom has no ResizeObserver; one measurement is all a test needs anyway.
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(publish)
    observer?.observe(bar)
    return () => {
      observer?.disconnect()
      root.style.removeProperty('--reader-chrome-offset')
    }
  }, [])

  return ref
}

/**
 * Copies the current URL for the ⋯ row; the label confirms, then falls back to
 * its resting text after a moment.
 */
function useCopyLink() {
  const { t } = useTranslation()
  const [status, setStatus] = useState<'idle' | 'copied' | 'failed'>('idle')
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    return () => {
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current)
      }
    }
  }, [])

  const resetTimer = (reset: () => void) => {
    if (timerRef.current !== null) {
      clearTimeout(timerRef.current)
    }
    timerRef.current = setTimeout(reset, 1500)
  }

  const copy = () => {
    setStatus('idle')
    const write = navigator.clipboard?.writeText(window.location.href)
    if (write === undefined) {
      setStatus('failed')
      resetTimer(() => {
        setStatus('idle')
      })
      return
    }
    void write.then(
      () => {
        setStatus('copied')
        resetTimer(() => {
          setStatus('idle')
        })
      },
      () => {
        setStatus('failed')
        resetTimer(() => {
          setStatus('idle')
        })
      },
    )
  }

  const label =
    status === 'copied'
      ? t('reader.linkCopied')
      : status === 'failed'
        ? t('reader.copyFailed')
        : t('reader.copyLink')

  const icon =
    status === 'copied' ? (
      <path d="m3.5 8.5 3 3 6-7" />
    ) : (
      <path d="M6.5 9.5 13 3M5 7V3.5A1.5 1.5 0 0 1 6.5 2h4A1.5 1.5 0 0 1 12 3.5V5m-1 5v4a1.5 1.5 0 0 1-1.5 1.5h-4A1.5 1.5 0 0 1 4 14v-4a1.5 1.5 0 0 1 1.5-1.5H7" />
    )

  return { label, icon, copy }
}

/** The writer's primary action, the one pill that stays beside the title. */
function ChromePill({ label, onClick, children }: { label: string; onClick: () => void; children: ReactNode }) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      onClick={onClick}
      className="inline-flex items-center gap-1.5 rounded-lg border border-line px-2.5 py-1.5 text-xs font-medium text-muted transition-colors hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
    >
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
        {children}
      </svg>
      <span className="hidden sm:inline">{label}</span>
    </button>
  )
}
