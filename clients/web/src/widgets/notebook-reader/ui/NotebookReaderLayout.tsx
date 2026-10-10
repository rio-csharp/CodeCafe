import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router'
import type { NotebookDetails, PageTreeNode } from '@/entities/notebook'
import type { MovePageData } from '@/entities/page'
import { ContentSkeleton, TreeSkeleton } from './ReaderSkeletons'
import { FavoritePagesSection } from './FavoritePagesSection'
import { PageDeleteDialog } from './PageDeleteDialog'
import { PageTrashDialog } from './PageTrashDialog'
import { PageTree } from './PageTree'
import { ReaderChrome } from './ReaderChrome'
import { RightPanel } from './RightPanel'
import { toPageHref } from '../lib/tree'
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
  /** Edit affordance in the chrome; hidden while null. */
  canEdit?: boolean
  onEdit?: () => void
  /** Page-favorite toggle rendered in the chrome; hidden while undefined. */
  favoriteAction?: ReactNode
  /** Markdown export of the open page; anonymous readers may use it too. */
  onExportPage?: () => void
  /** Page creation from the tree header (root) or a node (subpage); writers only. */
  onAddPage?: (parentPath?: string) => void
  /** Markdown import from the tree header (root) or a node menu (subpage); writers only. */
  onImportPage?: (parentPath?: string) => void
  /** Move/reorder from tree drag-and-drop; writers only. */
  onMovePage?: (pageId: string, data: MovePageData) => void
  /** Archive/unarchive from the node menu; writers only. */
  onToggleArchive?: (node: PageTreeNode) => void
  /** Soft-delete after the confirm dialog; writers only. */
  onDeletePage?: (node: PageTreeNode) => void
  /** Page-level sharing dialog; writers only, so undefined hides the pill. */
  onSharePage?: () => void
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
  canEdit = false,
  onEdit,
  favoriteAction,
  onExportPage,
  onAddPage,
  onImportPage,
  onMovePage,
  onToggleArchive,
  onDeletePage,
  onSharePage,
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
  // Tree title filter, client-side: the whole tree is already here.
  const [treeQuery, setTreeQuery] = useState('')
  const [trashOpen, setTrashOpen] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<PageTreeNode | null>(null)
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
        } min-h-0 flex-col border-r border-line bg-canvas lg:static lg:z-auto lg:flex lg:w-auto lg:shadow-none`}
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
              to={`/notebooks/${encodeURIComponent(notebook.slug)}`}
              className="block truncate text-sm font-semibold text-ink transition-colors hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
            >
              {notebook.title}
            </Link>
            <p className="text-[11px] text-muted">{t('reader.pageCount', { count: notebook.pageCount })}</p>
          </div>
          {canEdit ? (
            <button
              type="button"
              aria-label={t('reader.pageTrash')}
              title={t('reader.pageTrash')}
              onClick={() => {
                setTrashOpen(true)
              }}
              className="grid size-8 shrink-0 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
            >
              <svg viewBox="0 0 16 16" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M2.5 4h11M6 4V2.8A.8.8 0 0 1 6.8 2h2.4a.8.8 0 0 1 .8.8V4M4 4l.6 8.6A1.4 1.4 0 0 0 6 14h4a1.4 1.4 0 0 0 1.4-1.4L12 4M6.6 6.8v4.2M9.4 6.8v4.2" />
              </svg>
            </button>
          ) : null}
          {canEdit && onImportPage !== undefined ? (
            <button
              type="button"
              aria-label={t('reader.importPage')}
              title={t('reader.importPage')}
              onClick={() => {
                onImportPage()
              }}
              className="grid size-8 shrink-0 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
            >
              <svg viewBox="0 0 16 16" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                <path d="M8 10V2.5M5.5 5 8 2.5 10.5 5M2.5 10.5v2a1 1 0 0 0 1 1h9a1 1 0 0 0 1-1v-2" />
              </svg>
            </button>
          ) : null}
          {canEdit && onAddPage !== undefined ? (
            <button
              type="button"
              aria-label={t('reader.addPage')}
              title={t('reader.addPage')}
              onClick={() => {
                onAddPage()
              }}
              className="grid size-8 shrink-0 place-items-center rounded-full text-muted transition-colors hover:bg-muted-soft hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent"
            >
              <svg viewBox="0 0 16 16" className="size-4" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" aria-hidden="true">
                <path d="M8 3.5v9M3.5 8h9" />
              </svg>
            </button>
          ) : null}
          {mobilePanel === 'tree' ? <DrawerCloseButton ref={drawerCloseRef} onClose={closeDrawer} /> : null}
        </div>
        <div className="border-b border-line px-3 py-2">
          <label className="relative block">
            <span className="sr-only">{t('reader.searchPages')}</span>
            <svg
              viewBox="0 0 16 16"
              className="pointer-events-none absolute top-1/2 left-2.5 size-3.5 -translate-y-1/2 text-muted"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.6"
              strokeLinecap="round"
              aria-hidden="true"
            >
              <circle cx="7" cy="7" r="4.5" />
              <path d="m10.5 10.5 3 3" />
            </svg>
            <input
              type="search"
              value={treeQuery}
              placeholder={t('reader.searchPages')}
              onChange={(event) => {
                setTreeQuery(event.target.value)
              }}
              className="w-full rounded-md border border-line bg-card py-1.5 pr-2 pl-8 text-sm text-ink placeholder:text-muted focus:outline focus:outline-2 focus:outline-accent"
            />
          </label>
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <FavoritePagesSection
            notebookId={notebook.id}
            activePath={activePath}
            onNavigate={closeDrawer}
          />
          <PageTree
            slug={notebook.slug}
            roots={roots}
            activePath={activePath}
            filter={treeQuery}
            canWrite={canEdit}
            onMovePage={canEdit ? onMovePage : undefined}
            onToggleArchive={canEdit ? onToggleArchive : undefined}
            onImportSubpage={
              canEdit && onImportPage !== undefined
                ? (node) => {
                    onImportPage(node.path)
                  }
                : undefined
            }
            onDeletePage={
              canEdit && onDeletePage !== undefined
                ? (node) => {
                    setPendingDelete(node)
                  }
                : undefined
            }
            onAddChild={
              canEdit && onAddPage !== undefined
                ? (parentPath) => {
                    onAddPage(parentPath)
                  }
                : undefined
            }
            onNavigate={closeDrawer}
          />
        </div>
      </aside>

      {/* The text column. */}
      <main ref={mainRef} className="min-h-0 min-w-0 overflow-y-auto bg-card">
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
              canEdit={canEdit}
              onEdit={onEdit}
              favoriteAction={favoriteAction}
              onExportPage={onExportPage}
              onSharePage={onSharePage}
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
        } min-h-0 flex-col border-l border-line bg-canvas lg:static lg:z-auto lg:flex lg:w-auto lg:shadow-none`}
      >
        {mobilePanel === 'right' ? (
          <div className="flex justify-end border-b border-line px-2 py-1.5 lg:hidden">
            <DrawerCloseButton ref={drawerCloseRef} onClose={closeDrawer} />
          </div>
        ) : null}
        <RightPanel tabs={rightTabs} />
      </aside>

      {/* Mobile: panel toggles pinned to the corners, prev/next in the middle. */}
      <div className="pointer-events-none fixed inset-x-4 bottom-4 z-30 flex items-center justify-between gap-2 lg:hidden">
        <div className="pointer-events-auto">
          <ToolbarPill
            label={t('reader.contents')}
            pressed={mobilePanel === 'tree'}
            onClick={() => {
              setMobilePanel((panel) => (panel === 'tree' ? 'none' : 'tree'))
            }}
            icon={<path d="M3 3.5h10M3 8h10M3 12.5h10" />}
          />
        </div>
        <div className="pointer-events-auto flex items-center gap-2">
          {prevPage !== null ? (
            <ToolbarPill
              label={t('reader.previousPage', { title: prevPage.title })}
              onClick={() => {
                void navigate(toPageHref(notebook.slug, prevPage.path))
              }}
              icon={<path d="M9.5 3.5 5.5 8l4 4.5" />}
              text={prevPage.title}
            />
          ) : null}
          {nextPage !== null ? (
            <ToolbarPill
              label={t('reader.nextPage', { title: nextPage.title })}
              onClick={() => {
                void navigate(toPageHref(notebook.slug, nextPage.path))
              }}
              icon={<path d="m6.5 3.5 4 4.5-4 4.5" />}
              text={nextPage.title}
              iconLast
            />
          ) : null}
        </div>
        <div className="pointer-events-auto">
          <ToolbarPill
            label={t('reader.outline')}
            pressed={mobilePanel === 'right'}
            onClick={() => {
              setMobilePanel((panel) => (panel === 'right' ? 'none' : 'right'))
            }}
            icon={<path d="M13 3.5v9M3 3.5h6M3 8h6M3 12.5h6" />}
          />
        </div>
      </div>

      {canEdit && trashOpen ? (
        <PageTrashDialog
          slug={notebook.slug}
          onClose={() => {
            setTrashOpen(false)
          }}
        />
      ) : null}

      {canEdit && pendingDelete !== null ? (
        <PageDeleteDialog
          node={pendingDelete}
          onCancel={() => {
            setPendingDelete(null)
          }}
          onConfirm={() => {
            onDeletePage?.(pendingDelete)
            setPendingDelete(null)
          }}
        />
      ) : null}

      {/* Backdrop behind an open drawer; the close button and Escape are the
          accessible paths, this is the convenient one. */}
      {mobilePanel !== 'none' ? (
        <div aria-hidden="true" className="fixed inset-0 z-20 bg-black/20 lg:hidden" onClick={closeDrawer} />
      ) : null}
    </div>
  )
}

/** The floating toolbar's pill: an icon, plus an optional label that only wider phones show. */
function ToolbarPill({
  label,
  pressed,
  onClick,
  icon,
  text,
  iconLast = false,
}: {
  label: string
  pressed?: boolean
  onClick: () => void
  icon: ReactNode
  text?: string
  iconLast?: boolean
}) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      aria-pressed={pressed}
      onClick={onClick}
      className={`flex items-center gap-1.5 rounded-full border border-line bg-card px-3 py-2 text-xs font-medium text-ink shadow-lg transition-colors hover:bg-muted-soft focus-visible:outline focus-visible:outline-2 focus-visible:outline-accent ${
        iconLast ? 'flex-row-reverse' : ''
      }`}
    >
      <svg viewBox="0 0 16 16" className="size-3.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
        {icon}
      </svg>
      {text !== undefined ? (
        <span className="hidden max-w-20 truncate sm:inline">{text}</span>
      ) : null}
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
      <aside className="hidden min-h-0 flex-col border-r border-line bg-canvas lg:flex">
        <div className="border-b border-line px-3 py-3">
          <div className="h-4 w-2/3 animate-pulse rounded bg-line" />
          <div className="mt-1.5 h-2.5 w-1/4 animate-pulse rounded bg-line" />
        </div>
        <div className="border-b border-line px-3 py-2">
          <div className="h-[34px] animate-pulse rounded-md bg-line" />
        </div>
        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <TreeSkeleton />
        </div>
      </aside>
      <main className="min-h-0 min-w-0 overflow-y-auto bg-card">
        <div className="mx-auto w-full max-w-3xl px-4 py-6 sm:px-8 xl:px-12">
          <ContentSkeleton />
        </div>
      </main>
      <aside className="hidden border-l border-line bg-canvas lg:block" />
    </div>
  )
}
