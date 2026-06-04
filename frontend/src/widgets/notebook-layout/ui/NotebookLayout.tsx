import { useState, type ReactNode, type RefObject } from 'react'
import { PanelLeft, PanelRight, X } from 'lucide-react'

interface NotebookLayoutProps {
  topBar: ReactNode
  tree: ReactNode
  content: ReactNode
  rightPanel: ReactNode
  contentRef?: RefObject<HTMLElement | null>
}

export default function NotebookLayout({ topBar, tree, content, rightPanel, contentRef }: NotebookLayoutProps) {
  const [mobilePanel, setMobilePanel] = useState<'none' | 'tree' | 'right'>('none')

  const closePanel = () => setMobilePanel('none')

  return (
    <div className="h-screen flex flex-col bg-surface-elevated">
      {topBar}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-[280px_1fr_300px] min-h-0">
        {/* Tree - desktop always visible, mobile conditional */}
        <aside className={`${mobilePanel === 'tree' ? 'fixed inset-y-0 left-0 z-40 w-[280px] border-r border-border-default' : 'hidden'} lg:flex lg:static lg:z-auto flex-col bg-surface border-r border-border-default overflow-hidden`}>
          <div className="lg:hidden flex items-center justify-end p-2 border-b border-border-subtle">
            <button type="button" onClick={closePanel} className="p-1.5 rounded-md hover:bg-surface-hover" aria-label="Close panel">
              <X className="h-4 w-4 text-text-secondary" />
            </button>
          </div>
          {tree}
        </aside>

        {/* Content */}
        <main ref={contentRef} className="overflow-y-auto min-h-0 bg-surface relative">
          {/* Mobile panel toggles */}
          <div className="lg:hidden fixed bottom-4 left-4 right-4 flex items-center justify-between z-30 pointer-events-none">
            <button
              type="button"
              onClick={() => setMobilePanel(mobilePanel === 'tree' ? 'none' : 'tree')}
              className="pointer-events-auto flex items-center gap-1.5 rounded-full bg-surface border border-border-default shadow-lg px-3 py-2 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
              aria-label="Toggle contents panel"
            >
              <PanelLeft className="h-3.5 w-3.5" />
              Contents
            </button>
            <button
              type="button"
              onClick={() => setMobilePanel(mobilePanel === 'right' ? 'none' : 'right')}
              className="pointer-events-auto flex items-center gap-1.5 rounded-full bg-surface border border-border-default shadow-lg px-3 py-2 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
              aria-label="Toggle outline panel"
            >
              Outline
              <PanelRight className="h-3.5 w-3.5" />
            </button>
          </div>
          {content}
        </main>

        {/* Right Panel: Outline + AI - desktop always visible, mobile conditional */}
        <aside className={`${mobilePanel === 'right' ? 'fixed inset-y-0 right-0 z-40 w-[300px] border-l border-border-default' : 'hidden'} lg:flex lg:static lg:z-auto flex-col bg-surface border-l border-border-default overflow-hidden`}>
          <div className="lg:hidden flex items-center justify-end p-2 border-b border-border-subtle">
            <button type="button" onClick={closePanel} className="p-1.5 rounded-md hover:bg-surface-hover" aria-label="Close panel">
              <X className="h-4 w-4 text-text-secondary" />
            </button>
          </div>
          {rightPanel}
        </aside>
      </div>

      {/* Mobile overlay */}
      {mobilePanel !== 'none' && (
        <div className="lg:hidden fixed inset-0 z-30 bg-black/20" onClick={closePanel} />
      )}
    </div>
  )
}
