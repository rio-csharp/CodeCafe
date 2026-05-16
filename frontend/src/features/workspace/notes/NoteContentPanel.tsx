import { noteContent, tagColorMap } from './notesData'

export function NoteContentPanel() {
  return (
    <div className="flex flex-1 flex-col overflow-auto bg-bg/20">
      <div className="flex-1 p-6 lg:p-8">
        <div className="mx-auto max-w-[640px]">
          {/* Note header */}
          <div className="mb-6">
            <div className="mb-2 flex items-center justify-between">
              <h2 className="m-0 text-xl font-bold">{noteContent.title}</h2>
              <div className="flex items-center gap-2">
                <button className="text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4L18.5 2.5z"/></svg>
                </button>
                <button className="text-muted transition hover:text-text">
                  <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="1"/><circle cx="19" cy="12" r="1"/><circle cx="5" cy="12" r="1"/></svg>
                </button>
              </div>
            </div>
            <div className="flex items-center gap-3 text-xs text-muted">
              <span className={`rounded px-1.5 py-0.5 text-[10px] font-bold ${tagColorMap[noteContent.tag]}`}>{noteContent.tag}</span>
              <span className="flex items-center gap-1">
                <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
                Updated {noteContent.updatedAt}
              </span>
              <span className="flex items-center gap-1">
                <img src="https://github.com/rio-csharp.png" alt="" className="h-4 w-4 rounded-full" />
                {noteContent.author}
              </span>
            </div>
          </div>

          {/* Note body */}
          <div className="flex flex-col gap-6">
            {noteContent.sections.map((section, i) => (
              <div key={i}>
                <h3 className={`m-0 mb-2 text-sm font-bold ${section.heading === 'Out of Scope (for now)' ? 'text-accent' : ''}`}>{section.heading}</h3>

                {section.type === 'paragraph' && (
                  <>
                    <p className="m-0 text-sm leading-relaxed text-muted">{section.content}</p>
                    {section.list && (
                      <ul className="m-0 mt-2 flex flex-col gap-1 pl-4 text-sm text-muted">
                        {section.list.map((item, j) => (
                          <li key={j} className="leading-snug">{item}</li>
                        ))}
                      </ul>
                    )}
                  </>
                )}

                {section.type === 'checklist' && (
                  <div className="flex flex-col gap-2">
                    {section.items?.map((item, j) => (
                      <div key={j} className="flex items-center gap-2 text-sm">
                        <span className={`inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-sm border ${item.done ? 'border-success bg-success/20 text-success' : 'border-border'}`}>
                          {item.done && <svg viewBox="0 0 24 24" width="10" height="10" fill="none" stroke="currentColor" strokeWidth="3"><polyline points="20 6 9 17 4 12"/></svg>}
                        </span>
                        <span className={item.done ? 'text-muted line-through' : 'text-text'}>{item.text}</span>
                      </div>
                    ))}
                  </div>
                )}

                {section.type === 'bullet' && (
                  <ul className="m-0 flex flex-col gap-1 pl-4 text-sm text-muted">
                    {section.items?.map((item, j) => (
                      <li key={j} className="leading-snug">{item}</li>
                    ))}
                  </ul>
                )}
              </div>
            ))}
          </div>

          {/* Note footer */}
          <div className="mt-8 flex items-center justify-between border-t border-border pt-4">
            <div className="flex items-center gap-2 text-muted">
              <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg transition hover:bg-accent/8 hover:text-text">
                <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="10"/><path d="M8 14s1.5 2 4 2 4-2 4-2"/><line x1="9" y1="9" x2="9.01" y2="9"/><line x1="15" y1="9" x2="15.01" y2="9"/></svg>
              </button>
              <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg transition hover:bg-accent/8 hover:text-text">
                <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48"/></svg>
              </button>
              <button className="inline-flex h-8 w-8 items-center justify-center rounded-lg transition hover:bg-accent/8 hover:text-text">
                <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2"><path d="M14 9V5a3 3 0 0 0-3-3l-4 9v11h11.28a2 2 0 0 0 2-1.7l1.38-9a2 2 0 0 0-2-2.3zM7 22H4a2 2 0 0 1-2-2v-7a2 2 0 0 1 2-2h3"/></svg>
              </button>
            </div>
            <div className="flex items-center gap-2 text-xs text-muted">
              <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" strokeWidth="2"><polyline points="20 6 9 17 4 12"/></svg>
              Saved {noteContent.updatedAt}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
