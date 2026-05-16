import { pinnedNotes, recentActivity, tagColorMap } from './notesData'

function ActivityIcon({ type }: { type: string }) {
  if (type === 'edit') return <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4L18.5 2.5z"/></svg>
  if (type === 'pin') return <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>
  if (type === 'brain') return <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2z"/><path d="M12 6a6 6 0 1 0 6 6 6 6 0 0 0-6-6z"/></svg>
  return <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg>
}

export function NotesRightSidebar() {
  return (
    <aside className="hidden w-[280px] shrink-0 flex-col gap-5 overflow-auto border-l border-border bg-bg/40 p-5 xl:flex">
      {/* Pinned Notes */}
      <div>
        <div className="mb-3 flex items-center justify-between">
          <h3 className="m-0 flex items-center gap-2 text-sm font-bold">
            <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>
            Pinned Notes
          </h3>
          <a href="#" className="text-xs font-medium text-accent no-underline">View all</a>
        </div>
        <div className="flex flex-col gap-2">
          {pinnedNotes.map((note, i) => (
            <button key={i} className="flex items-center gap-2 rounded-lg border border-border bg-bg/60 p-2 text-left text-sm transition hover:bg-accent/8">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
              <span className="flex-1 truncate">{note.title}</span>
              <span className={`shrink-0 rounded px-1.5 py-0.5 text-[10px] font-bold ${tagColorMap[note.tag] || 'bg-accent/15 text-accent'}`}>{note.tag}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Recent Activity */}
      <div>
        <div className="mb-3 flex items-center justify-between">
          <h3 className="m-0 text-sm font-bold">Recent Activity</h3>
          <a href="#" className="text-xs font-medium text-accent no-underline">View all</a>
        </div>
        <div className="flex flex-col gap-3">
          {recentActivity.map((act, i) => (
            <div key={i} className="flex items-start gap-2">
              <div className="mt-0.5 text-muted">
                <ActivityIcon type={act.icon} />
              </div>
              <div className="min-w-0 flex-1">
                <span className="text-xs text-muted">You {act.action} </span>
                <span className="text-xs">{act.target}</span>
                <div className="text-[11px] text-muted">{act.time}</div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* AI Note Assistant */}
      <div className="mt-auto rounded-xl border border-accent/20 bg-accent/5 p-4">
        <div className="mb-1 flex items-center gap-2 text-sm font-bold">
          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" className="text-accent"><path d="M12 2L15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>
          AI Note Assistant
        </div>
        <p className="m-0 mb-3 text-xs leading-relaxed text-muted">Get AI help to summarize, expand, or find connections in your notes.</p>
        <button className="flex w-full items-center justify-center gap-2 rounded-lg bg-accent px-4 py-2.5 text-sm font-semibold text-[#070a12] transition hover:opacity-90">
          Ask AI about my notes
          <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>
        </button>
      </div>
    </aside>
  )
}
