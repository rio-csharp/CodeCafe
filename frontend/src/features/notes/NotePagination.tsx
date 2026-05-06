import type { NoteSummary } from './notesApi'

export function NotePagination({
  activeIndex,
  className,
  label,
  noteCount,
  nextNote,
  onMove,
  previousNote,
}: {
  activeIndex: number
  className: string
  label: string
  noteCount: number
  nextNote: NoteSummary | null
  onMove: (path: string) => void
  previousNote: NoteSummary | null
}) {
  return (
    <nav className={className} aria-label={label}>
      <button disabled={!previousNote} onClick={() => previousNote && onMove(previousNote.path)} type="button">
        Previous
      </button>
      <span>
        {activeIndex + 1 > 0 ? activeIndex + 1 : 0} / {noteCount}
      </span>
      <button disabled={!nextNote} onClick={() => nextNote && onMove(nextNote.path)} type="button">
        Next
      </button>
    </nav>
  )
}
