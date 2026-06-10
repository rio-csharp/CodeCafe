import { useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import type { NotebookItem } from '@/entities/notebook-item'

interface NotebookPageNavigationProps {
  notebookSlug: string
  prev: NotebookItem | null
  next: NotebookItem | null
}

export default function NotebookPageNavigation({ notebookSlug, prev, next }: NotebookPageNavigationProps) {
  const navigate = useNavigate()

  if (!prev && !next) return null

  const handleNavigate = (path: string) => {
    navigate(`/notes/${notebookSlug}/${path}`)
    window.scrollTo(0, 0)
  }

  return (
    <nav className="mt-8 pt-6 border-t border-border-subtle">
      <div className="flex items-center justify-between gap-4">
        {prev ? (
          <button
            type="button"
            onClick={() => handleNavigate(prev.path)}
            className="group flex items-center gap-2 text-left rounded-lg border border-border-subtle px-4 py-3 text-sm text-text-secondary hover:bg-surface-hover hover:text-text-primary transition-colors max-w-[50%]"
            title={prev.title}
          >
            <ChevronLeft className="h-4 w-4 shrink-0 text-text-tertiary group-hover:text-text-primary transition-colors" />
            <span className="truncate">{prev.title}</span>
          </button>
        ) : (
          <div />
        )}

        {next ? (
          <button
            type="button"
            onClick={() => handleNavigate(next.path)}
            className="group flex items-center gap-2 text-right rounded-lg border border-border-subtle px-4 py-3 text-sm text-text-secondary hover:bg-surface-hover hover:text-text-primary transition-colors max-w-[50%]"
            title={next.title}
          >
            <span className="truncate">{next.title}</span>
            <ChevronRight className="h-4 w-4 shrink-0 text-text-tertiary group-hover:text-text-primary transition-colors" />
          </button>
        ) : (
          <div />
        )}
      </div>
    </nav>
  )
}
