import { useState } from 'react'
import { WorkspaceSidebar } from './WorkspaceSidebar'
import { NotesCategoryPanel } from './notes/NotesCategoryPanel'
import { NotesListPanel } from './notes/NotesListPanel'
import { NoteContentPanel } from './notes/NoteContentPanel'
import { NotesRightSidebar } from './notes/NotesRightSidebar'
import { recentNotes } from './notes/notesData'
import type { RecentNote } from './notes/notesData'

export function WorkspaceNotesPage() {
  const [selectedNote, setSelectedNote] = useState<RecentNote>(recentNotes[0])
  const [activeCategory, setActiveCategory] = useState('All Notes')

  return (
    <div className="flex min-h-screen bg-bg text-text">
      <WorkspaceSidebar activeItem="Notes" />

      <div className="flex flex-1 flex-col">
        {/* Header */}
        <header className="flex items-center justify-between border-b border-border px-6 py-4">
          <div>
            <h1 className="m-0 text-2xl font-bold tracking-tight">Notes</h1>
            <p className="m-0 mt-1 text-sm text-muted">Capture ideas, decisions, and important context for CodeCafe.</p>
          </div>
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2 rounded-lg border border-border bg-bg/60 px-3 py-1.5">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
              <input type="text" placeholder="Search notes..." className="border-0 bg-transparent text-sm text-text outline-none placeholder:text-muted" />
              <span className="rounded border border-border px-1 py-0.5 text-[10px] text-muted">⌘K</span>
            </div>
            <button className="inline-flex items-center gap-1.5 rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-[#070a12] transition hover:opacity-90">
              <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
              New Note
            </button>
          </div>
        </header>

        {/* Content */}
        <div className="flex flex-1 overflow-hidden">
          <NotesCategoryPanel activeCategory={activeCategory} onSelect={setActiveCategory} />
          <NotesListPanel selectedNote={selectedNote} onSelect={setSelectedNote} />
          <NoteContentPanel />
          <NotesRightSidebar />
        </div>
      </div>
    </div>
  )
}
