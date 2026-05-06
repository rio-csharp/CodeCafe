type OutlineItem = {
  depth: number
  id: string
  text: string
}

export function NoteOutline({
  items,
  onItemClick,
}: {
  items: OutlineItem[]
  onItemClick: (id: string) => void
}) {
  return (
    <aside className="note-outline" aria-label="Note outline">
      <div className="note-outline-header">Outline</div>
      {items.length > 0 ? (
        <nav className="note-outline-list">
          {items.map((item) => (
            <a
              className={`note-outline-item depth-${Math.min(item.depth, 3)}`}
              href={`#${item.id}`}
              key={item.id}
              onClick={(event) => {
                event.preventDefault()
                onItemClick(item.id)
              }}
            >
              {item.text}
            </a>
          ))}
        </nav>
      ) : (
        <p className="empty-settings-copy note-outline-empty">No headings found.</p>
      )}
    </aside>
  )
}
