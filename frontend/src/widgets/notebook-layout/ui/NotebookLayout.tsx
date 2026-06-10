import { useEffect, useRef, useState, type ReactNode, type RefObject } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight, PanelLeft, PanelRight, X } from 'lucide-react'
import type { NotebookItem } from '@/entities/notebook-item'

interface NotebookLayoutProps {
  topBar?: ReactNode
  tree: ReactNode
  content: ReactNode
  rightPanel: ReactNode
  contentRef?: RefObject<HTMLElement | null>
  notebookSlug?: string
  prevPage?: NotebookItem | null
  nextPage?: NotebookItem | null
}

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'

export default function NotebookLayout({ topBar, tree, content, rightPanel, contentRef, notebookSlug, prevPage, nextPage }: NotebookLayoutProps) {
  const navigate = useNavigate()
  const [mobilePanel, setMobilePanel] = useState<'none' | 'tree' | 'right'>('none')
  const treePanelRef = useRef<HTMLElement>(null)
  const rightPanelRef = useRef<HTMLElement>(null)
  const treeCloseButtonRef = useRef<HTMLButtonElement>(null)
  const rightCloseButtonRef = useRef<HTMLButtonElement>(null)
  const previousFocusRef = useRef<HTMLElement | null>(null)

  const closePanel = () => setMobilePanel('none')

  useEffect(() => {
    if (mobilePanel === 'none') return

    previousFocusRef.current = document.activeElement as HTMLElement | null
    const panel = mobilePanel === 'tree' ? treePanelRef.current : rightPanelRef.current
    const closeButton = mobilePanel === 'tree' ? treeCloseButtonRef.current : rightCloseButtonRef.current
    const focusTarget = closeButton ?? panel?.querySelector<HTMLElement>(FOCUSABLE_SELECTOR)
    focusTarget?.focus()

    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        e.preventDefault()
        closePanel()
        return
      }

      if (e.key !== 'Tab' || !panel) return

      const focusable = Array.from(panel.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
      if (focusable.length === 0) {
        e.preventDefault()
        return
      }

      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      const active = document.activeElement as HTMLElement | null

      if (e.shiftKey && active === first) {
        e.preventDefault()
        last.focus()
      } else if (!e.shiftKey && active === last) {
        e.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      previousFocusRef.current?.focus()
    }
  }, [mobilePanel])

  return (
    <div className="h-screen flex flex-col bg-surface-elevated">
      {topBar}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-[280px_1fr_300px] min-h-0">
        {/* Tree - desktop always visible, mobile conditional */}
        <aside
          ref={treePanelRef}
          className={`${mobilePanel === 'tree' ? 'fixed inset-y-0 left-0 z-40 w-[280px] border-r border-border-default' : 'hidden'} lg:flex lg:static lg:z-auto flex-col bg-surface border-r border-border-default overflow-hidden`}
        >
          <div className="lg:hidden flex items-center justify-end p-2 border-b border-border-subtle">
            <button ref={treeCloseButtonRef} type="button" onClick={closePanel} className="p-1.5 rounded-md hover:bg-surface-hover" aria-label="Close panel">
              <X className="h-4 w-4 text-text-secondary" />
            </button>
          </div>
          {tree}
        </aside>

        {/* Content */}
        <main ref={contentRef} className="overflow-y-auto min-h-0 bg-surface relative">
          {/* Mobile bottom toolbar */}
          <div className="lg:hidden fixed bottom-4 left-4 right-4 flex items-center justify-between gap-2 z-30 pointer-events-none">
            <div className="flex items-center gap-2 pointer-events-auto">
              {prevPage && notebookSlug && (
                <button
                  type="button"
                  onClick={() => { navigate(`/notes/${notebookSlug}/${prevPage.path}`); window.scrollTo(0, 0) }}
                  className="flex items-center gap-1 rounded-full bg-surface border border-border-default shadow-lg px-3 py-2 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                  title={prevPage.title}
                  aria-label={`Previous: ${prevPage.title}`}
                >
                  <ChevronLeft className="h-3.5 w-3.5" />
                  <span className="hidden sm:inline max-w-[80px] truncate">{prevPage.title}</span>
                </button>
              )}
            </div>
            <div className="flex items-center gap-2 pointer-events-auto">
              <button
                type="button"
                onClick={() => setMobilePanel(mobilePanel === 'tree' ? 'none' : 'tree')}
                className="flex items-center gap-1.5 rounded-full bg-surface border border-border-default shadow-lg px-3 py-2 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                aria-label="Toggle contents panel"
              >
                <PanelLeft className="h-3.5 w-3.5" />
                Contents
              </button>
              <button
                type="button"
                onClick={() => setMobilePanel(mobilePanel === 'right' ? 'none' : 'right')}
                className="flex items-center gap-1.5 rounded-full bg-surface border border-border-default shadow-lg px-3 py-2 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                aria-label="Toggle outline panel"
              >
                Outline
                <PanelRight className="h-3.5 w-3.5" />
              </button>
            </div>
            <div className="flex items-center gap-2 pointer-events-auto">
              {nextPage && notebookSlug && (
                <button
                  type="button"
                  onClick={() => { navigate(`/notes/${notebookSlug}/${nextPage.path}`); window.scrollTo(0, 0) }}
                  className="flex items-center gap-1 rounded-full bg-surface border border-border-default shadow-lg px-3 py-2 text-xs font-medium text-text-secondary hover:bg-surface-hover transition-colors"
                  title={nextPage.title}
                  aria-label={`Next: ${nextPage.title}`}
                >
                  <span className="hidden sm:inline max-w-[80px] truncate">{nextPage.title}</span>
                  <ChevronRight className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
          </div>
          {content}
        </main>

        {/* Right Panel: Outline + AI - desktop always visible, mobile conditional */}
        <aside
          ref={rightPanelRef}
          className={`${mobilePanel === 'right' ? 'fixed inset-y-0 right-0 z-40 w-[300px] border-l border-border-default' : 'hidden'} lg:flex lg:static lg:z-auto flex-col bg-surface border-l border-border-default overflow-hidden`}
        >
          <div className="lg:hidden flex items-center justify-end p-2 border-b border-border-subtle">
            <button ref={rightCloseButtonRef} type="button" onClick={closePanel} className="p-1.5 rounded-md hover:bg-surface-hover" aria-label="Close panel">
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
