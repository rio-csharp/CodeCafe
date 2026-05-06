import { NoteTree } from './noteTree'
import type { NoteTreeNode } from './noteTreeBuilder'

const bookDownloadLinks = [
  {
    href: 'https://github.com/rio-csharp/Notes/releases/download/latest/notes.pdf',
    label: 'PDF',
  },
  {
    href: 'https://github.com/rio-csharp/Notes/releases/download/latest/notes.epub',
    label: 'EPUB',
  },
] as const

export function NotesSidebar({
  activePath,
  expandedPaths,
  isLoadingList,
  nodes,
  onQueryChange,
  onSelect,
  onToggleDirectory,
  query,
}: {
  activePath: string
  expandedPaths: Set<string>
  isLoadingList: boolean
  nodes: NoteTreeNode[]
  onQueryChange: (value: string) => void
  onSelect: (path: string) => void
  onToggleDirectory: (path: string, isOpen: boolean) => void
  query: string
}) {
  return (
    <aside className="notes-sidebar" aria-label="Notes list">
      <div className="notes-sidebar-header">
        <input
          aria-label="Search notes"
          onChange={(event) => onQueryChange(event.target.value)}
          placeholder="Search notes"
          type="search"
          value={query}
        />
      </div>

      <div className="note-list">
        <NoteTree
          activePath={activePath}
          expandedPaths={expandedPaths}
          nodes={nodes}
          onSelect={onSelect}
          onToggleDirectory={onToggleDirectory}
        />

        {!isLoadingList && nodes.length === 0 ? (
          <p className="empty-settings-copy note-list-empty">No notes found.</p>
        ) : null}
      </div>

      <div className="notes-downloads" aria-label="Download notes book">
        {bookDownloadLinks.map((link) => (
          <a
            className="notes-download-link"
            href={link.href}
            key={link.label}
            rel="noreferrer"
            target="_blank"
          >
            {link.label}
          </a>
        ))}
      </div>
    </aside>
  )
}
