import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import type { NotebookDetails, PageTreeNode } from '@/entities/notebook'
import { ContentSkeleton, TreeSkeleton } from './ReaderSkeletons'
import { PageTree } from './PageTree'
import { ReaderChrome } from './ReaderChrome'
import { RightPanel } from './RightPanel'
import type { RightPanelTab } from './RightPanel'

/** A flat pointer to a page — what the prev/next pills need. */
export interface ReaderPageRef {
  path: string
  title: string
}

export interface NotebookReaderLayoutProps {
  notebook: NotebookDetails
  roots: readonly PageTreeNode[]
  /** The open page's path, or null on the notebook root. */
  activePath?: string | null
  /** Right-panel tabs; the outline today, pinned chat later. */
  rightTabs?: readonly RightPanelTab[]
  /** The open page's title; the sticky in-text chrome only shows with one. */
  pageTitle?: string | null
  refreshing?: boolean
  onRefresh?: () => void
  /** Linear reading order neighbours; the mobile toolbar shows them. */
  prevPage?: ReaderPageRef | null
  nextPage?: ReaderPageRef | null
  children: ReactNode
}

type MobilePanel = 'none' | 'tree' | 'right'

/**
 * The reading surface in the 3.4 mold: no top bar at all — the tree panel's
 * header carries the way home and the notebook's name. Three fixed columns
 * on desktop (tree / text / tabbed panel); on phones the columns become
 * drawers above a floating bottom toolbar. <main> is the only scroller.
 */
export function NotebookReaderLayout({
  notebook,
  roots,
  activePath = null,
  rightTabs = [],
  pageTitle = null,
  refreshing = false,
  onRefresh,
  prevPage = null,
  nextPage = null,
  children,
}: NotebookReaderLayoutProps) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [mobilePanel, setMobilePanel] = useState<MobilePanel>('none')
  // Narrow reads better; the chrome's pill widens when wanted. Not persisted.
  const [contentWide, setContentWide] = useState(false)
  const mainRef = useRef<HTMLElement>(null)
  const drawerCloseRef = useRef<HTMLButtonElement>(null)
  const restoreFocusRef = useRef<HTMLElement | null>(null)

  // <main> is the scroll container (the window never scrolls in this shell),
  // so page-to-page navigation must reset it explicitly.
  useEffect(() => {
    // Optional call: jsdom does not implement Element.scrollTo.
    mainRef.current?.scrollTo?.(0, 0)
  }, [activePath])

  // Drawer manners: Escape closes, focus lands on the drawer's close button
  // and returns to whatever had it before.
  useEffect(() => {
    if (mobilePanel === 'none') {
      return
    }
    restoreFocusRef.current = document.activeElement as HTMLElement | null
    drawerCloseRef.current?.focus()

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setMobilePanel('none')
      }
    }
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      restoreFocusRef.current?.focus()
    }
  }, [mobilePanel])

  const closeDrawer = () => {
    setMobilePanel('none')
  }

  return (
    <div className="grid h-dvh grid-cols-1 bg-canvas lg:grid-cols-[280px_minmax(0,1fr)_300px]">
      {/* Page tree: a drawer below lg, a column from lg up. */}
      <aside
        className={`${
          mobilePanel === 'tree'
            ? 'fixed inset-y-0 left-0 z-40 flex w-[280px] shadow-2xl'
            : 'hidden'
        } min-h-0 flex-col border-r border-line bg-card lg:static lg:z-auto lg:flex lg:w-auto lg:shadow-none`}
      >
        <div className="flex items-center gap-2 border-b border-line px-3 py-3">
          <Link
            to="/"
            aria-label={t('reader.backHome')}
            title={t('reader.backHome')}
            className="grid size-8 shrink-0 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
          >
            <svg viewBox="0 0 16 16" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
              <path d="M10 3 5 8l5 5" />
            </svg>
          </Link>
          <div className="min-w-0 flex-1">
            <Link
              to={`/${notebook.slug}`}
              className="block truncate text-sm font-semibold text-ink transition-colors hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
            >
              {notebook.title}
            </Link>
            <p className="text-[11px] text-muted">{t('reader.pageCount', { count: notebook.pageCount })}</p>
          </div>
          {mobilePanel === 'tree' ? <DrawerCloseButton ref={drawerCloseRef} onClose={closeDrawer} /> : null}
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <PageTree slug={notebook.slug} roots={roots} activePath={activePath} onNavigate={closeDrawer} />
        </div>
      </aside>

      {/* The text column. */}
      <main ref={mainRef} className="min-h-0 min-w-0 overflow-y-auto">
        <div
          className={`px-4 py-6 pb-24 sm:px-8 lg:pb-6 xl:px-12 ${
            contentWide ? '' : 'mx-auto w-full max-w-3xl'
          }`}
        >
          {pageTitle !== null ? (
            <ReaderChrome
              title={pageTitle}
              refreshing={refreshing}
              onRefresh={() => {
                onRefresh?.()
              }}
              wide={contentWide}
              onToggleWide={() => {
                setContentWide((wide) => !wide)
              }}
            />
          ) : null}
          {children}
        </div>
      </main>

      {/* Right panel: a drawer below lg, a column from lg up. */}
      <aside
        className={`${
          mobilePanel === 'right'
            ? 'fixed inset-y-0 right-0 z-40 flex w-[300px] shadow-2xl'
            : 'hidden'
        } min-h-0 flex-col border-l border-line bg-card lg:static lg:z-auto lg:flex lg:w-auto lg:shadow-none`}
      >
        {mobilePanel === 'right' ? (
          <div className="flex justify-end border-b border-line px-2 py-1.5 lg:hidden">
            <DrawerCloseButton ref={drawerCloseRef} onClose={closeDrawer} />
          </div>
        ) : null}
        <RightPanel tabs={rightTabs} />
      </aside>

      {/* Mobile: prev / panel toggles / next, floating over the text. */}
      <div className="pointer-events-none fixed inset-x-4 bottom-4 z-30 flex items-center justify-between gap-2 lg:hidden">
        <div className="pointer-events-auto">
          {prevPage !== null ? (
            <ToolbarPill
              label={t('reader.previousPage', { title: prevPage.title })}
              onClick={() => {
                void navigate(`/${notebook.slug}/${prevPage.path}`)
              }}
            >
              <path d="M9.5 3.5 5.5 8l4 4.5" />
              <span className="hidden max-w-20 truncate sm:inline">{prevPage.title}</span>
            </ToolbarPill>
          ) : null}
        </div>
        <div className="pointer-events-auto flex items-center gap-2">
          <ToolbarPill
            label={t('reader.contents')}
            pressed={mobilePanel === 'tree'}
            onClick={() => {
              setMobilePanel((panel) => (panel === 'tree' ? 'none' : 'tree'))
            }}
          >
            <path d="M3 3.5h10M3 8h10M3 12.5h10" />
            <span>{t('reader.contents')}</span>
          </ToolbarPill>
          <ToolbarPill
            label={t('reader.outline')}
            pressed={mobilePanel === 'right'}
            onClick={() => {
              setMobilePanel((panel) => (panel === 'right' ? 'none' : 'right'))
            }}
          >
            <span>{t('reader.outline')}</span>
            <path d="M13 3.5v9M3 3.5h6M3 8h6M3 12.5h6" />
          </ToolbarPill>
        </div>
        <div className="pointer-events-auto">
          {nextPage !== null ? (
            <ToolbarPill
              label={t('reader.nextPage', { title: nextPage.title })}
              onClick={() => {
                void navigate(`/${notebook.slug}/${nextPage.path}`)
              }}
            >
              <span className="hidden max-w-20 truncate sm:inline">{nextPage.title}</span>
              <path d="m6.5 3.5 4 4.5-4 4.5" />
            </ToolbarPill>
          ) : null}
        </div>
      </div>

      {/* Backdrop behind an open drawer; the close button and Escape are the
          accessible paths, this is the convenient one. */}
      {mobilePanel !== 'none' ? (
        <div aria-hidden="true" className="fixed inset-0 z-20 bg-black/20 lg:hidden" onClick={closeDrawer} />
      ) : null}
    </div>
  )
}

/** The floating toolbar's pill. Children are icon paths plus an optional label span. */
function ToolbarPill({
  label,
  pressed,
  onClick,
  children,
}: {
  label: string
  pressed?: boolean
  onClick: () => void
  children: ReactNode
}) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      aria-pressed={pressed}
      onClick={onClick}
      className="flex items-center gap-1.5 rounded-full border border-line bg-card px-3 py-2 text-xs font-medium text-ink shadow-lg transition-colors hover:bg-muted-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
    >
      <svg viewBox="0 0 16 16" className="size-3.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        {children}
      </svg>
      {children}
    </button>
  )
}

/** The × at the top of a mobile drawer. */
function DrawerCloseButton({ ref, onClose }: { ref: React.Ref<HTMLButtonElement>; onClose: () => void }) {
  const { t } = useTranslation()
  return (
    <button
      ref={ref}
      type="button"
      aria-label={t('dialog.close')}
      onClick={onClose}
      className="grid size-8 shrink-0 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-ink focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent lg:hidden"
    >
      <svg viewBox="0 0 16 16" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" aria-hidden="true">
        <path d="m4 4 8 8M12 4l-8 8" />
      </svg>
    </button>
  )
}

/** The same shell before the reads land — no notebook to name yet. */
export function ReaderSkeletonLayout() {
  return (
    <div className="grid h-dvh grid-cols-1 bg-canvas lg:grid-cols-[280px_minmax(0,1fr)_300px]">
      <aside className="hidden min-h-0 flex-col border-r border-line bg-card lg:flex">
        <div className="border-b border-line px-3 py-3">
          <div className="h-4 w-2/3 animate-pulse rounded bg-line" />
          <div className="mt-1.5 h-2.5 w-1/4 animate-pulse rounded bg-line" />
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <TreeSkeleton />
        </div>
      </aside>
      <main className="min-h-0 min-w-0 overflow-y-auto">
        <div className="mx-auto w-full max-w-3xl px-4 py-6 sm:px-8 xl:px-12">
          <ContentSkeleton />
        </div>
      </main>
      <aside className="hidden border-l border-line bg-card lg:block" />
    </div>
  )
}
