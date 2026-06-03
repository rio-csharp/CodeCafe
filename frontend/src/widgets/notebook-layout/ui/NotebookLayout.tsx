import type { ReactNode, RefObject } from 'react'

interface NotebookLayoutProps {
  topBar: ReactNode
  tree: ReactNode
  content: ReactNode
  rightPanel: ReactNode
  contentRef?: RefObject<HTMLElement | null>
}

export default function NotebookLayout({ topBar, tree, content, rightPanel, contentRef }: NotebookLayoutProps) {
  return (
    <div className="h-screen flex flex-col bg-surface-elevated">
      {topBar}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-[280px_1fr_300px] min-h-0">
        {/* Tree */}
        <aside className="hidden lg:flex flex-col border-r border-border-default overflow-hidden">
          {tree}
        </aside>

        {/* Content */}
        <main ref={contentRef} className="overflow-y-auto min-h-0 bg-surface">
          {content}
        </main>

        {/* Right Panel: Outline + AI */}
        <aside className="hidden lg:flex flex-col border-l border-border-default overflow-hidden">
          {rightPanel}
        </aside>
      </div>
    </div>
  )
}
