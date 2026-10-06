import { useState } from 'react'
import type { ReactNode } from 'react'
import type { PageTreeNode } from '@/entities/notebook'
import type { NotebookDetails } from '@/entities/notebook'
import { Container } from '@/shared/ui'
import { SiteFooter } from '@/widgets/site-footer'
import { SiteHeader } from '@/widgets/site-header'
import { ContentSkeleton, TreeSkeleton } from './ReaderSkeletons'
import { NotebookHeader } from './NotebookHeader'
import { PageTree } from './PageTree'

/**
 * The chrome every reader state shares. Callers hand over their own `<aside>`
 * and header because their visibility rules differ, not their geometry.
 */
function ReaderShell({
  header,
  aside,
  children,
}: {
  header: ReactNode
  aside: ReactNode
  children: ReactNode
}) {
  return (
    <div className="flex min-h-dvh flex-col bg-canvas">
      <SiteHeader />
      {header}

      <Container className="flex flex-1 flex-col gap-8 py-8 md:flex-row">
        {aside}
        <main className="min-w-0 max-w-3xl flex-1">{children}</main>
      </Container>

      <SiteFooter />
    </div>
  )
}

export interface NotebookReaderLayoutProps {
  notebook: NotebookDetails
  roots: readonly PageTreeNode[]
  /** The open page's path, or null on the notebook root. */
  activePath?: string | null
  children: ReactNode
}

/**
 * The loaded chrome. From `md` up the tree is a sticky sidebar; below that it
 * lives behind the header's Contents disclosure.
 */
export function NotebookReaderLayout({
  notebook,
  roots,
  activePath = null,
  children,
}: NotebookReaderLayoutProps) {
  const [contentsOpen, setContentsOpen] = useState(false)

  return (
    <ReaderShell
      header={
        <NotebookHeader
          notebook={notebook}
          contentsOpen={contentsOpen}
          onToggleContents={() => {
            setContentsOpen((open) => !open)
          }}
        />
      }
      aside={
        <aside
          className={`${contentsOpen ? 'block' : 'hidden'} w-full shrink-0 md:block md:w-64`}
        >
          <div className="md:sticky md:top-20">
            <PageTree
              slug={notebook.slug}
              roots={roots}
              activePath={activePath}
              onNavigate={() => {
                setContentsOpen(false)
              }}
            />
          </div>
        </aside>
      }
    >
      {children}
    </ReaderShell>
  )
}

/** Chrome for the reads that have not landed yet — there is no notebook to show. */
export function ReaderSkeletonLayout() {
  return (
    <ReaderShell
      header={
        <div className="border-b border-line bg-card">
          <Container className="flex flex-col gap-3 py-8">
            <div className="h-8 w-2/3 animate-pulse rounded bg-line" />
            <div className="h-4 w-1/2 animate-pulse rounded bg-line" />
          </Container>
        </div>
      }
      aside={
        <aside className="hidden w-full shrink-0 md:block md:w-64">
          <TreeSkeleton />
        </aside>
      }
    >
      <ContentSkeleton />
    </ReaderShell>
  )
}
