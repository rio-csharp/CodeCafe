import { formatRelativeTime, summarizePreview } from './chatUtils'
import type { ChatSession } from './chatTypes'

export function ChatSessionList({
  effectiveSelectedSessionId,
  filteredSessions,
  isBackendHealthy,
  onDeleteSession,
  onSearchTextChange,
  onSelectSession,
  onStartNewChat,
  searchText,
}: {
  effectiveSelectedSessionId: string | null
  filteredSessions: ChatSession[]
  isBackendHealthy: boolean
  onDeleteSession: (sessionId: string) => void
  onSearchTextChange: (value: string) => void
  onSelectSession: (session: ChatSession) => void
  onStartNewChat: () => void
  searchText: string
}) {
  return (
    <aside className="session-list" aria-label="Conversations">
      <header className="session-list-header">
        <label className="sr-only" htmlFor="session-search">
          Search conversations
        </label>
        <input
          id="session-search"
          onChange={(event) => onSearchTextChange(event.target.value)}
          placeholder="Search"
          type="search"
          value={searchText}
        />
        <button className="icon-button" onClick={onStartNewChat} type="button">
          New
        </button>
        <span
          aria-label={isBackendHealthy ? 'Backend healthy' : 'Backend unhealthy'}
          className={isBackendHealthy ? 'health-dot ready' : 'health-dot offline'}
          role="status"
        />
      </header>

      <div className="session-items">
        {filteredSessions.length > 0 ? (
          filteredSessions.map((session) => (
            <div
              aria-current={session.id === effectiveSelectedSessionId ? 'true' : undefined}
              className="session-item"
              key={session.id}
            >
              <button
                aria-current={session.id === effectiveSelectedSessionId ? 'true' : undefined}
                aria-label={`Open ${session.title}`}
                className="session-item-main"
                onClick={() => onSelectSession(session)}
                type="button"
              >
                <span className="session-item-copy">
                  <strong>{session.title}</strong>
                  <small>{summarizePreview(session.messages.at(-1)?.text ?? 'No messages yet.')}</small>
                </span>
                <time>{formatRelativeTime(session.updatedAt)}</time>
              </button>

              <button
                aria-label={`Delete ${session.title}`}
                className="icon-button session-delete-button"
                onClick={() => onDeleteSession(session.id)}
                type="button"
              >
                ×
              </button>
            </div>
          ))
        ) : (
          <p className="empty-settings-copy session-list-empty">No conversations yet.</p>
        )}
      </div>
    </aside>
  )
}
