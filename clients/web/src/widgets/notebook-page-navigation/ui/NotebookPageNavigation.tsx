import { Link } from 'react-router-dom'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import type { NotebookItem } from '@/entities/notebook-item'

interface NotebookPageNavigationProps {
  notebookSlug: string
  prev: NotebookItem | null
  next: NotebookItem | null
}

// Scroll reset on navigation is handled centrally by NotebookLayout, which
// scrolls the actual scroll container (<main>) on pathname change.
export default function NotebookPageNavigation({ notebookSlug, prev, next }: NotebookPageNavigationProps) {
  if (!prev && !next) return null

  return (
    <nav className="hidden lg:block mt-8 pt-6 pb-6 border-t border-border-subtle">
      <div className="flex items-center justify-between gap-4">
        {prev ? (
          <Link
            to={`/notes/${notebookSlug}/${prev.path}`}
            className="group flex items-center gap-2 text-left rounded-lg border border-border-subtle px-4 py-3 text-sm text-text-secondary hover:bg-surface-hover hover:text-text-primary transition-colors max-w-[50%]"
            title={prev.title}
          >
            <ChevronLeft className="h-4 w-4 shrink-0 text-text-tertiary group-hover:text-text-primary transition-colors" />
            <span className="truncate">{prev.title}</span>
          </Link>
        ) : (
          <div />
        )}

        {next ? (
          <Link
            to={`/notes/${notebookSlug}/${next.path}`}
            className="group flex items-center gap-2 text-right rounded-lg border border-border-subtle px-4 py-3 text-sm text-text-secondary hover:bg-surface-hover hover:text-text-primary transition-colors max-w-[50%]"
            title={next.title}
          >
            <span className="truncate">{next.title}</span>
            <ChevronRight className="h-4 w-4 shrink-0 text-text-tertiary group-hover:text-text-primary transition-colors" />
          </Link>
        ) : (
          <div />
        )}
      </div>
    </nav>
  )
}
