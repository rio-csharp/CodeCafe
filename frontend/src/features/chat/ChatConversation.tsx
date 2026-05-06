import { Link } from 'react-router-dom'
import { MarkdownContent } from '../../components/MarkdownContent'
import type { AiModelOption } from '../ai/aiSettingsStore'
import type { ChatSession } from './chatTypes'

export function ChatConversation({
  enabledModelOptions,
  isSettingsOpen,
  onOpenSettingsToggle,
  onSelectModelValue,
  onSelectSessionBack,
  selectedModelOption,
  selectedProviderConfigured,
  selectedSession,
}: {
  enabledModelOptions: AiModelOption[]
  isSettingsOpen: boolean
  onOpenSettingsToggle: () => void
  onSelectModelValue: (value: string | null) => void
  onSelectSessionBack: () => void
  selectedModelOption: AiModelOption | null
  selectedProviderConfigured: boolean
  selectedSession: ChatSession | null
}) {
  return (
    <>
      <header className="console-header chat-console-header">
        <button
          className="mobile-back-button"
          onClick={onSelectSessionBack}
          type="button"
        >
          Back
        </button>
        <div className="console-title chat-console-title">
          <h2>{selectedSession?.title ?? 'New chat'}</h2>
          <div className="chat-console-meta">
            <select
              aria-label="Model"
              className="chat-select"
              onChange={(event) => onSelectModelValue(event.target.value || null)}
              value={selectedModelOption?.value ?? ''}
            >
              {enabledModelOptions.length > 0 ? (
                enabledModelOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))
              ) : (
                <option value="">No model</option>
              )}
            </select>

            <button
              aria-expanded={isSettingsOpen}
              aria-label="Chat settings"
              className="icon-button toolbar-icon-button"
              onClick={onOpenSettingsToggle}
              type="button"
              title="Chat settings"
            >
              <span aria-hidden="true">⚙</span>
            </button>
          </div>
        </div>
      </header>

      {selectedSession ? (
        <div className="message-thread" aria-label={`${selectedSession.title} messages`}>
          {selectedSession.messages.map((message) => (
            <article className={`message-bubble chat-message-bubble ${message.role}`} key={message.id}>
              {message.text ? (
                <MarkdownContent>{message.text}</MarkdownContent>
              ) : null}
            </article>
          ))}
        </div>
      ) : (
        <div className="empty-thread" aria-label="Empty conversation">
          <h3>Start a chat.</h3>
          <p>
            Pick a model, then ask about code, notes, architecture, or anything else you want
            the workspace to help with.
          </p>
          {!selectedProviderConfigured ? (
            <p>
              AI is not configured yet. <Link to="/settings/ai">Open AI settings</Link>.
            </p>
          ) : null}
        </div>
      )}
    </>
  )
}
