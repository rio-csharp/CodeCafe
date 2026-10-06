import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import type { PageTreeNode } from '@/entities/notebook'
import type { NotebookDetails } from '@/entities/notebook'
import { ContentSkeleton, TreeSkeleton } from './ReaderSkeletons'
import { NotebookTopBar } from './NotebookTopBar'
import { PageTree } from './PageTree'
import { ReaderChrome } from './ReaderChrome'
import { RightPanel } from './RightPanel'
import type { RightPanelTab } from './RightPanel'

/** Panels are bands above the text below `md`, then fixed side columns. */
const PANEL_CLASS = {
  tree: 'flex-col overflow-y-auto border-line bg-card max-h-[40vh] md:max-h-none md:h-full md:w-[17rem] md:shrink-0 md:border-r',
  right:
    'flex-col overflow-hidden border-line bg-card max-h-[40vh] md:max-h-none md:h-full md:w-[16rem] md:shrink-0 md:border-l',
} as const

/**
 * Below `md` a panel is a disclosure (closed by default); from `md` up it is a
 * column (open by default). One button drives both, so the class carries both
 * breakpoints and only the applicable one is ever visible.
 */
function panelClass(desktopOpen: boolean, mobileOpen: boolean, base: string): string {
  return `${mobileOpen ? 'flex' : 'hidden'} ${desktopOpen ? 'md:flex' : 'md:hidden'} ${base}`
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
  children: ReactNode
}

/**
 * The reading surface: one strip of chrome up top, then tree / text / tabbed
 * side panel. Height is fixed and each pane scrolls itself, so nothing moves
 * while reading.
 */
export function NotebookReaderLayout({
  notebook,
  roots,
  activePath = null,
  rightTabs = [],
  pageTitle = null,
  refreshing = false,
  onRefresh,
  children,
}: NotebookReaderLayoutProps) {
  const [treeOpen, setTreeOpen] = useState(true)
  const [rightOpen, setRightOpen] = useState(true)
  const [mobilePanel, setMobilePanel] = useState<'none' | 'tree' | 'right'>('none')
  // Not persisted: the project keeps localStorage down to lang/theme/refresh.
  const [contentWide, setContentWide] = useState(true)
  const mainRef = useRef<HTMLElement>(null)

  // <main> is the scroll container (the window never scrolls in this shell),
  // so page-to-page navigation must reset it explicitly — otherwise a deep
  // page opens wherever the previous one left off.
  useEffect(() => {
    // Optional call: jsdom does not implement Element.scrollTo.
    mainRef.current?.scrollTo?.(0, 0)
  }, [activePath])

  return (
    <div className="flex h-dvh flex-col bg-canvas">
      <NotebookTopBar
        notebook={notebook}
        treeOpen={treeOpen}
        onToggleTree={() => {
          setTreeOpen((open) => !open)
          setMobilePanel((panel) => (panel === 'tree' ? 'none' : 'tree'))
        }}
        rightOpen={rightOpen}
        onToggleRight={() => {
          setRightOpen((open) => !open)
          setMobilePanel((panel) => (panel === 'right' ? 'none' : 'right'))
        }}
      />

      <div className="flex min-h-0 flex-1 flex-col overflow-y-auto md:flex-row md:overflow-hidden">
        <aside className={panelClass(treeOpen, mobilePanel === 'tree', PANEL_CLASS.tree)}>
          <div className="p-4">
            <PageTree
              slug={notebook.slug}
              roots={roots}
              activePath={activePath}
              onNavigate={() => {
                // The mobile band would keep covering the text after a tap.
                setMobilePanel('none')
              }}
            />
          </div>
        </aside>

        <main
          ref={mainRef}
          className={`min-w-0 flex-1 px-4 py-6 sm:px-8 md:overflow-y-auto xl:px-12 ${
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
        </main>

        <aside className={panelClass(rightOpen, mobilePanel === 'right', PANEL_CLASS.right)}>
          <RightPanel tabs={rightTabs} />
        </aside>
      </div>
    </div>
  )
}

/** The same shell before the reads land — no notebook to name yet. */
export function ReaderSkeletonLayout() {
  return (
    <div className="flex h-dvh flex-col bg-canvas">
      <div className="h-14 shrink-0 border-b border-line bg-card" />

      <div className="flex min-h-0 flex-1 flex-col overflow-y-auto md:flex-row md:overflow-hidden">
        <aside className={panelClass(true, false, PANEL_CLASS.tree)}>
          <div className="p-4">
            <TreeSkeleton />
          </div>
        </aside>

        <main className="min-w-0 flex-1 px-4 py-6 sm:px-8 md:overflow-y-auto xl:px-12">
          <ContentSkeleton />
        </main>

        <aside className={panelClass(true, false, PANEL_CLASS.right)} />
      </div>
    </div>
  )
}
