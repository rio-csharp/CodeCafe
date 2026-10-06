import { useState } from 'react'
import type { ReactNode } from 'react'
import type { PageTreeNode } from '@/entities/notebook'
import type { NotebookDetails } from '@/entities/notebook'
import { ContentSkeleton, TreeSkeleton } from './ReaderSkeletons'
import { NotebookTopBar } from './NotebookTopBar'
import { PageTree } from './PageTree'

/** Panels are bands above the text below `md`, then fixed side columns. */
const PANEL_CLASS = {
  tree: 'flex-col overflow-y-auto border-line bg-card max-h-[40vh] md:max-h-none md:h-full md:w-[17rem] md:shrink-0 md:border-r',
  outline:
    'flex-col overflow-y-auto border-line bg-card max-h-[40vh] md:max-h-none md:h-full md:w-[16rem] md:shrink-0 md:border-l',
} as const

/**
 * Below `md` a panel is a disclosure (closed by default); from `md` up it is a
 * column (open by default). One button drives both, so the class carries both
 * breakpoints and only the applicable one is ever visible.
 */
function panelClass(desktopOpen: boolean, mobileOpen: boolean, base: string): string {
  return `${mobileOpen ? 'flex' : 'hidden'} ${desktopOpen ? 'md:flex' : 'md:hidden'} ${base}`
}

/**
 * The reading surface: one strip of chrome, then tree / text / outline. Height
 * is fixed and each pane scrolls itself, so nothing moves while reading.
 */
function ReaderShell({
  topBar,
  treeClassName,
  tree,
  outlineClassName,
  outline,
  mainClassName,
  children,
}: {
  topBar: ReactNode
  treeClassName: string
  tree: ReactNode
  outlineClassName: string
  outline: ReactNode
  mainClassName: string
  children: ReactNode
}) {
  return (
    <div className="flex h-dvh flex-col bg-canvas">
      {topBar}

      <div className="flex min-h-0 flex-1 flex-col overflow-y-auto md:flex-row md:overflow-hidden">
        <aside className={treeClassName}>{tree}</aside>

        <main className={`min-w-0 flex-1 px-4 py-6 sm:px-8 xl:px-12 ${mainClassName}`}>
          {children}
        </main>

        <aside className={outlineClassName}>{outline}</aside>
      </div>
    </div>
  )
}

export interface NotebookReaderLayoutProps {
  notebook: NotebookDetails
  roots: readonly PageTreeNode[]
  /** The open page's path, or null on the notebook root. */
  activePath?: string | null
  /** Right panel contents; the page derives it from the block tree. */
  outline?: ReactNode
  children: ReactNode
}

export function NotebookReaderLayout({
  notebook,
  roots,
  activePath = null,
  outline,
  children,
}: NotebookReaderLayoutProps) {
  const [treeOpen, setTreeOpen] = useState(true)
  const [outlineOpen, setOutlineOpen] = useState(true)
  const [mobilePanel, setMobilePanel] = useState<'none' | 'tree' | 'outline'>('none')
  // Not persisted: the project keeps localStorage down to lang/theme/refresh.
  const [contentWide, setContentWide] = useState(true)

  return (
    <ReaderShell
      mainClassName={
        contentWide ? 'md:overflow-y-auto' : 'mx-auto w-full max-w-3xl md:overflow-y-auto'
      }
      topBar={
        <NotebookTopBar
          notebook={notebook}
          treeOpen={treeOpen}
          onToggleTree={() => {
            setTreeOpen((open) => !open)
            setMobilePanel((panel) => (panel === 'tree' ? 'none' : 'tree'))
          }}
          outlineOpen={outlineOpen}
          onToggleOutline={() => {
            setOutlineOpen((open) => !open)
            setMobilePanel((panel) => (panel === 'outline' ? 'none' : 'outline'))
          }}
          contentWide={contentWide}
          onToggleContentWidth={() => {
            setContentWide((wide) => !wide)
          }}
        />
      }
      treeClassName={panelClass(treeOpen, mobilePanel === 'tree', PANEL_CLASS.tree)}
      tree={
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
      }
      outlineClassName={panelClass(outlineOpen, mobilePanel === 'outline', PANEL_CLASS.outline)}
      outline={outline}
    >
      {children}
    </ReaderShell>
  )
}

/** The same shell before the reads land — no notebook to name yet. */
export function ReaderSkeletonLayout() {
  return (
    <ReaderShell
      mainClassName="md:overflow-y-auto"
      topBar={<div className="h-14 shrink-0 border-b border-line bg-card" />}
      treeClassName={panelClass(true, false, PANEL_CLASS.tree)}
      tree={
        <div className="p-4">
          <TreeSkeleton />
        </div>
      }
      outlineClassName={panelClass(true, false, PANEL_CLASS.outline)}
      outline={null}
    >
      <ContentSkeleton />
    </ReaderShell>
  )
}
