import { categories, tags } from './notesData'

function NoteCategoryIcon({ type }: { type: string }) {
  if (type === 'doc') return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
  if (type === 'decision') return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
  if (type === 'idea') return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z"/></svg>
  if (type === 'plan') return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>
  if (type === 'research') return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
  return <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
}

export function NotesCategoryPanel({
  activeCategory,
  onSelect,
}: {
  activeCategory: string
  onSelect: (name: string) => void
}) {
  return (
    <aside className="flex w-[200px] shrink-0 flex-col gap-1 overflow-auto border-r border-border bg-bg/40 p-4">
      <div className="mb-2 px-2">
        <div className="text-sm font-bold">All Notes</div>
        <div className="text-xs text-muted">28 notes</div>
      </div>
      {categories.map((cat) => (
        <button
          key={cat.name}
          onClick={() => onSelect(cat.name)}
          className={`flex items-center gap-2 rounded-lg px-2 py-1.5 text-left text-sm transition-colors ${
            activeCategory === cat.name ? 'bg-accent/10 text-text' : 'text-muted hover:bg-accent/8 hover:text-text'
          }`}
        >
          <NoteCategoryIcon type={cat.icon} />
          <span className="flex-1">{cat.name}</span>
          <span className="text-xs text-muted">{cat.count}</span>
        </button>
      ))}

      <div className="mt-4 px-2 text-[10px] font-bold tracking-widest text-muted uppercase">Tags</div>
      <div className="mt-1 flex flex-col gap-0.5">
        {tags.map((tag) => (
          <button key={tag.name} className="flex items-center justify-between rounded-lg px-2 py-1 text-left text-xs text-muted transition hover:bg-accent/8 hover:text-text">
            <span>{tag.name}</span>
            <span className="text-muted/60">{tag.count}</span>
          </button>
        ))}
        <button className="flex items-center gap-1 rounded-lg px-2 py-1 text-left text-xs text-accent">
          <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
          Add tag
        </button>
      </div>
    </aside>
  )
}
