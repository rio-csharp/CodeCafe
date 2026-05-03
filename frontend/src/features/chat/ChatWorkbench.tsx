import { useEffect, useState } from 'react'
import { checkBackendHealth } from '../../lib/apiClient'

const healthCheckIntervalMs = 5_000

type ChatSession = {
  id: string
  title: string
  summary: string
  lastMessageAt: string
  messages: Array<{
    author: 'user' | 'assistant'
    text: string
  }>
}

const chatSessions: ChatSession[] = [
  {
    id: 'deploy-review',
    title: 'Deployment review',
    summary: 'Check production rollout and rollback plan.',
    lastMessageAt: 'Now',
    messages: [
      {
        author: 'user',
        text: 'Review the deployment workflow and point out risky steps.',
      },
      {
        author: 'assistant',
        text: 'Start with image provenance, health checks, and rollback ownership.',
      },
    ],
  },
  {
    id: 'api-design',
    title: 'API design',
    summary: 'Shape the first chat endpoint contract.',
    lastMessageAt: '12m',
    messages: [
      {
        author: 'user',
        text: 'What should the first chat API look like?',
      },
      {
        author: 'assistant',
        text: 'Keep the first contract small: session id, message text, and streamed response later.',
      },
    ],
  },
  {
    id: 'notes-roadmap',
    title: 'Notes roadmap',
    summary: 'Move notes behind chat as a later workspace module.',
    lastMessageAt: '1h',
    messages: [
      {
        author: 'user',
        text: 'Where should notes fit after chat?',
      },
      {
        author: 'assistant',
        text: 'Treat notes as workspace context that chat can read and write after auth is stable.',
      },
    ],
  },
]

export function ChatWorkbench() {
  const [isBackendHealthy, setIsBackendHealthy] = useState(false)
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null)
  const selectedSession =
    chatSessions.find((session) => session.id === selectedSessionId) ?? null

  useEffect(() => {
    let ignoreResult = false

    async function refreshHealth() {
      try {
        const isHealthy = await checkBackendHealth()

        if (!ignoreResult) {
          setIsBackendHealthy(isHealthy)
        }
      } catch {
        if (!ignoreResult) {
          setIsBackendHealthy(false)
        }
      }
    }

    void refreshHealth()
    const intervalId = window.setInterval(refreshHealth, healthCheckIntervalMs)

    return () => {
      ignoreResult = true
      window.clearInterval(intervalId)
    }
  }, [])

  useEffect(() => {
    document.body.classList.toggle('chat-session-open', Boolean(selectedSession))

    return () => {
      document.body.classList.remove('chat-session-open')
    }
  }, [selectedSession])

  return (
    <section
      className={`chat-workbench${selectedSession ? ' has-active-session' : ''}`}
      aria-label="AI chat workspace"
    >
      <aside className="session-list" aria-label="Conversations">
        <header className="session-list-header">
          <label className="sr-only" htmlFor="session-search">
            Search conversations
          </label>
          <input
            id="session-search"
            placeholder="Search"
            type="search"
          />
          <span
            aria-label={isBackendHealthy ? 'Backend healthy' : 'Backend unhealthy'}
            className={isBackendHealthy ? 'health-dot ready' : 'health-dot offline'}
            role="status"
          />
        </header>

        <div className="session-items">
          {chatSessions.map((session) => (
            <button
              aria-current={session.id === selectedSessionId ? 'true' : undefined}
              className="session-item"
              key={session.id}
              onClick={() => setSelectedSessionId(session.id)}
              type="button"
            >
              <span>
                <strong>{session.title}</strong>
                <small>{session.summary}</small>
              </span>
              <time>{session.lastMessageAt}</time>
            </button>
          ))}
        </div>
      </aside>

      <section className="chat-console" aria-label="Conversation">
        {selectedSession ? (
          <>
            <header className="console-header">
              <button
                className="mobile-back-button"
                onClick={() => setSelectedSessionId(null)}
                type="button"
              >
                Back
              </button>
              <div className="console-title">
                <h2>{selectedSession.title}</h2>
              </div>
            </header>

            <div className="message-thread" aria-label={`${selectedSession.title} messages`}>
              {selectedSession.messages.map((message, index) => (
                <article className={`message-bubble ${message.author}`} key={index}>
                  <p>{message.text}</p>
                </article>
              ))}
            </div>

            <form className="chat-composer">
              <label className="sr-only" htmlFor="chat-message">
                Message
              </label>
              <textarea
                id="chat-message"
                name="message"
                placeholder="Ask about code, deployments, notes, or workspace context..."
                rows={1}
              />
              <button type="button">Send</button>
            </form>
          </>
        ) : (
          <div className="empty-thread" aria-label="Empty conversation">
            <h3>Choose a conversation.</h3>
            <p>Open a session to continue working with AI on code, deployments, notes, or context.</p>
          </div>
        )}
      </section>
    </section>
  )
}
