import { recentNotes, tagColorMap, type RecentNote } from './notesData'

export function NotesListPanel({
  selectedNote,
  onSelect,
}: {
  selectedNote: RecentNote
  onSelect: (note: RecentNote) => void
}) {
  return (
    <div className="flex w-[280px] shrink-0 flex-col overflow-auto border-r border-border bg-bg/20 p-4">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="m-0 text-sm font-bold">Recent Notes</h2>
        <button className="flex items-center gap-1 text-xs text-muted">
          Updated <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="6 9 12 15 18 9"/></svg>
        </button>
      </div>
      <div className="flex flex-col gap-2">
        {recentNotes.map((note) => (
          <button
            key={note.id}
            onClick={() => onSelect(note)}
            className={`rounded-lg border p-3 text-left transition ${
              selectedNote.id === note.id
                ? 'border-border bg-surface/60'
                : 'border-transparent hover:border-border hover:bg-surface/30'
            }`}
          >
            <div className="mb-1 flex items-start justify-between gap-2">
              <span className="text-sm font-semibold">{note.title}</span>
              {note.pinned && (
                <span className="text-accent">
                  <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>
                </span>
              )}
            </div>
            <p className="m-0 mb-2 text-xs leading-snug text-muted">{note.desc}</p>
            <div className="flex items-center justify-between">
              <span className="text-[11px] text-muted">{note.updatedAt}</span>
              <span className={`rounded px-1.5 py-0.5 text-[10px] font-bold ${tagColorMap[note.tag] || 'bg-accent/15 text-accent'}`}>{note.tag}</span>
            </div>
          </button>
        ))}
      </div>
    </div>
  )
}
