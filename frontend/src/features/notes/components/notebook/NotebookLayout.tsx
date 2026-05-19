import type { ReactNode } from 'react'

interface NotebookLayoutProps {
  topBar: ReactNode
  tree: ReactNode
  content: ReactNode
  rightPanel: ReactNode
}

export default function NotebookLayout({ topBar, tree, content, rightPanel }: NotebookLayoutProps) {
  return (
    <div className="h-screen flex flex-col bg-stone-50">
      {topBar}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-[280px_1fr_300px] min-h-0">
        {/* Tree */}
        <aside className="hidden lg:flex flex-col border-r border-stone-200 overflow-hidden">
          {tree}
        </aside>

        {/* Content */}
        <main className="overflow-y-auto min-h-0 bg-white">
          {content}
        </main>

        {/* Right Panel: Outline + AI */}
        <aside className="hidden lg:flex flex-col border-l border-stone-200 overflow-hidden">
          {rightPanel}
        </aside>
      </div>
    </div>
  )
}
