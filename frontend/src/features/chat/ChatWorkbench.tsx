import { useEffect, useMemo, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { streamChatResponse } from '../ai/aiClient'
import { MarkdownContent } from '../../components/MarkdownContent'
import {
  getDefaultModel,
  getDefaultProvider,
  loadAiSettings,
  type AiSettings,
} from '../ai/aiSettingsStore'
import { checkBackendHealth } from '../../lib/apiClient'

const healthCheckIntervalMs = 5_000
const chatSessionsStorageKey = 'codecafe-chat-sessions'
const chatPreferencesStorageKey = 'codecafe-chat-preferences'
const chatSelectedSessionStorageKey = 'codecafe-chat-selected-session'

type ChatPreferences = {
  maxOutputTokens: number | null
  systemPrompt: string
  temperature: number | null
  topP: number | null
}

type SessionMessage = {
  id: string
  role: 'assistant' | 'user'
  text: string
}

type ChatSession = {
  id: string
  modelId: string | null
  providerId: string | null
  title: string
  updatedAt: string
  messages: SessionMessage[]
}

export function ChatWorkbench() {
  const [aiSettings, setAiSettings] = useState<AiSettings>(() => loadAiSettings())
  const [chatPreferences, setChatPreferences] = useState<ChatPreferences>(() => loadChatPreferences())
  const [draftMessage, setDraftMessage] = useState('')
  const [isBackendHealthy, setIsBackendHealthy] = useState(false)
  const [isSettingsOpen, setIsSettingsOpen] = useState(false)
  const [searchText, setSearchText] = useState('')
  const [sendingSessionIds, setSendingSessionIds] = useState<string[]>([])
  const [selectedModelValue, setSelectedModelValue] = useState<string | null>(
    () => {
      const settings = loadAiSettings()
      const provider = getDefaultProvider(settings)
      const model = getDefaultModel(settings)

      return provider && model ? toModelOptionValue(provider.id, model.id) : null
    },
  )
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(() => loadSelectedSessionId())
  const [sessions, setSessions] = useState<ChatSession[]>(() => loadChatSessions())
  const [status, setStatus] = useState('')
  const abortControllersRef = useRef<Map<string, AbortController>>(new Map())
  const silentAbortSessionIdsRef = useRef<Set<string>>(new Set())

  const enabledModelOptions = useMemo(
    () =>
      aiSettings.providers
        .filter((provider) => provider.enabled)
        .flatMap((provider) =>
          provider.models
            .filter((model) => model.enabled)
            .map((model) => ({
              label: model.name,
              model,
              provider,
              value: toModelOptionValue(provider.id, model.id),
            })),
        ),
    [aiSettings.providers],
  )

  const defaultModelOption = useMemo(() => {
    if (!aiSettings.defaultProviderId || !aiSettings.defaultModelId) {
      return enabledModelOptions[0] ?? null
    }

    return (
      enabledModelOptions.find(
        (option) =>
          option.provider.id === aiSettings.defaultProviderId &&
          option.model.id === aiSettings.defaultModelId,
      ) ??
      enabledModelOptions[0] ??
      null
    )
  }, [aiSettings.defaultModelId, aiSettings.defaultProviderId, enabledModelOptions])

  const selectedModelOption = useMemo(() => {
    if (!selectedModelValue) {
      return defaultModelOption
    }

    return enabledModelOptions.find((option) => option.value === selectedModelValue) ?? defaultModelOption
  }, [defaultModelOption, enabledModelOptions, selectedModelValue])

  const selectedProvider = selectedModelOption?.provider ?? null
  const selectedModel = selectedModelOption?.model ?? null
  const selectedSessionExists = selectedSessionId
    ? sessions.some((session) => session.id === selectedSessionId)
    : false
  const effectiveSelectedSessionId = selectedSessionExists ? selectedSessionId : null
  const selectedSession = sessions.find((session) => session.id === effectiveSelectedSessionId) ?? null
  const isSelectedSessionSending = selectedSession
    ? sendingSessionIds.includes(selectedSession.id)
    : false

  const filteredSessions = useMemo(() => {
    const normalizedSearch = searchText.trim().toLowerCase()

    if (!normalizedSearch) {
      return sessions
    }

    return sessions.filter((session) =>
      session.title.toLowerCase().includes(normalizedSearch),
    )
  }, [searchText, sessions])

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
    const syncSettings = () => {
      const nextSettings = loadAiSettings()
      const nextOptions = nextSettings.providers
        .filter((provider) => provider.enabled)
        .flatMap((provider) =>
          provider.models
            .filter((model) => model.enabled)
            .map((model) => toModelOptionValue(provider.id, model.id)),
        )

      setAiSettings(nextSettings)
      setSelectedModelValue((currentModelValue) => {
        if (currentModelValue && nextOptions.includes(currentModelValue)) {
          return currentModelValue
        }

        const defaultProvider = getDefaultProvider(nextSettings)
        const defaultModel = getDefaultModel(nextSettings)

        return defaultProvider && defaultModel
          ? toModelOptionValue(defaultProvider.id, defaultModel.id)
          : nextOptions[0] ?? null
      })
    }

    syncSettings()
    window.addEventListener('storage', syncSettings)
    window.addEventListener('focus', syncSettings)

    return () => {
      window.removeEventListener('storage', syncSettings)
      window.removeEventListener('focus', syncSettings)
    }
  }, [])

  useEffect(() => {
    window.localStorage.setItem(chatSessionsStorageKey, JSON.stringify(sessions))
  }, [sessions])

  useEffect(() => {
    window.localStorage.setItem(chatPreferencesStorageKey, JSON.stringify(chatPreferences))
  }, [chatPreferences])

  useEffect(() => {
    if (isMobileViewport()) {
      window.localStorage.removeItem(chatSelectedSessionStorageKey)
      return
    }

    if (!selectedSessionId) {
      window.localStorage.removeItem(chatSelectedSessionStorageKey)
      return
    }

    window.localStorage.setItem(chatSelectedSessionStorageKey, selectedSessionId)
  }, [selectedSessionId])

  useEffect(() => {
    document.body.classList.toggle('chat-session-open', Boolean(selectedSession))

    return () => {
      document.body.classList.remove('chat-session-open')
    }
  }, [selectedSession])

  useEffect(() => {
    const abortControllers = abortControllersRef.current
    const silentAbortSessionIds = silentAbortSessionIdsRef.current

    return () => {
      for (const controller of abortControllers.values()) {
        controller.abort()
      }

      abortControllers.clear()
      silentAbortSessionIds.clear()
    }
  }, [])

  async function handleSendMessage() {
    const provider = selectedProvider
    const model = selectedModel
    const trimmedMessage = draftMessage.trim()
    const effectiveMaxOutputTokens = chatPreferences.maxOutputTokens ?? model?.defaultMaxOutputTokens ?? 2048
    const effectiveTemperature = chatPreferences.temperature ?? model?.defaultTemperature ?? 0.7
    const effectiveTopP = chatPreferences.topP ?? model?.defaultTopP ?? 1

    if (!provider || !model) {
      setStatus('Configure an enabled model in AI settings first.')
      return
    }

    if (!provider.baseUrl.trim() || !provider.apiKey.trim()) {
      setStatus('Add a base URL and API key before sending a message.')
      return
    }

    if (!trimmedMessage) {
      return
    }

    const now = new Date().toISOString()
    const targetSessionId = selectedSession?.id ?? crypto.randomUUID()

    if (sendingSessionIds.includes(targetSessionId)) {
      return
    }

    const userMessage: SessionMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      text: trimmedMessage,
    }
    const assistantMessage: SessionMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      text: '',
    }
    const nextSession = selectedSession
      ? {
          ...selectedSession,
          messages: [...selectedSession.messages, userMessage, assistantMessage],
          modelId: model.id,
          providerId: provider.id,
          title: selectedSession.messages.length === 0 ? summarizeTitle(trimmedMessage) : selectedSession.title,
          updatedAt: now,
        }
      : {
          id: targetSessionId,
          messages: [userMessage, assistantMessage],
          modelId: model.id,
          providerId: provider.id,
          title: summarizeTitle(trimmedMessage),
          updatedAt: now,
        }

    setSessions((currentSessions) => upsertSession(currentSessions, nextSession))
    setSelectedSessionId(targetSessionId)
    setDraftMessage('')
    setStatus('')
    setSendingSessionIds((currentSessionIds) => [...currentSessionIds, targetSessionId])

    const controller = new AbortController()
    abortControllersRef.current.set(targetSessionId, controller)

    try {
      await streamChatResponse({
        maxOutputTokens: effectiveMaxOutputTokens,
        messages: nextSession.messages,
        model,
        onDelta: (delta) => {
          setSessions((currentSessions) =>
            currentSessions.map((session) =>
              session.id === targetSessionId
                ? {
                    ...session,
                    messages: session.messages.map((message) =>
                      message.id === assistantMessage.id
                        ? {
                            ...message,
                            text: `${message.text}${delta}`,
                          }
                        : message,
                    ),
                    updatedAt: new Date().toISOString(),
                  }
                : session,
            ),
          )
        },
        provider,
        signal: controller.signal,
        systemPrompt: chatPreferences.systemPrompt,
        temperature: effectiveTemperature,
        topP: effectiveTopP,
      })
    } catch (error) {
      const shouldSuppressAbortStatus =
        error instanceof Error &&
        error.name === 'AbortError' &&
        silentAbortSessionIdsRef.current.has(targetSessionId)
      const message =
        error instanceof Error && error.name === 'AbortError'
          ? 'Generation stopped.'
          : error instanceof Error
            ? error.message
            : 'Request failed.'

      setSessions((currentSessions) =>
        currentSessions.map((session) =>
          session.id === targetSessionId
            ? {
                ...session,
                messages: session.messages.map((item) =>
                  item.id === assistantMessage.id
                    ? {
                        ...item,
                        text: item.text.length > 0 ? item.text : message,
                      }
                    : item,
                ),
                updatedAt: new Date().toISOString(),
              }
            : session,
        ),
      )

      if (!shouldSuppressAbortStatus) {
        setStatus(message)
      }
    } finally {
      abortControllersRef.current.delete(targetSessionId)
      silentAbortSessionIdsRef.current.delete(targetSessionId)
      setSendingSessionIds((currentSessionIds) =>
        currentSessionIds.filter((sessionId) => sessionId !== targetSessionId),
      )
    }
  }

  function startNewChat() {
    setSelectedSessionId(null)
    setDraftMessage('')
    setStatus('')
  }

  function deleteSession(sessionId: string) {
    const controller = abortControllersRef.current.get(sessionId)

    if (controller) {
      silentAbortSessionIdsRef.current.add(sessionId)
      controller.abort()
      abortControllersRef.current.delete(sessionId)
    }

    setSessions((currentSessions) => currentSessions.filter((session) => session.id !== sessionId))
    setSendingSessionIds((currentSessionIds) =>
      currentSessionIds.filter((currentSessionId) => currentSessionId !== sessionId),
    )
    setSelectedSessionId((currentSelectedSessionId) =>
      currentSelectedSessionId === sessionId ? null : currentSelectedSessionId,
    )
  }

  function stopGeneration() {
    if (!selectedSession) {
      return
    }

    abortControllersRef.current.get(selectedSession.id)?.abort()
  }

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
            onChange={(event) => setSearchText(event.target.value)}
            placeholder="Search"
            type="search"
            value={searchText}
          />
          <button className="icon-button" onClick={startNewChat} type="button">
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
                  onClick={() => {
                    setSelectedSessionId(session.id)
                    if (session.providerId && session.modelId) {
                      setSelectedModelValue(toModelOptionValue(session.providerId, session.modelId))
                    }
                  }}
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
                  onClick={() => deleteSession(session.id)}
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

      <section className="chat-console" aria-label="Conversation">
        <header className="console-header chat-console-header">
          <button
            className="mobile-back-button"
            onClick={() => setSelectedSessionId(null)}
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
                onChange={(event) => setSelectedModelValue(event.target.value || null)}
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
                onClick={() => setIsSettingsOpen((currentValue) => !currentValue)}
                type="button"
                title="Chat settings"
              >
                <span aria-hidden="true">⚙</span>
              </button>
            </div>
          </div>
        </header>

        {isSettingsOpen ? (
          <>
            <button
              aria-label="Close chat settings"
              className="chat-settings-backdrop"
              onClick={() => setIsSettingsOpen(false)}
              type="button"
            />
            <section className="chat-settings-popover" aria-label="Chat controls">
              <label className="chat-setting-field chat-setting-field-wide">
                <span>System prompt</span>
                <textarea
                  onChange={(event) =>
                    setChatPreferences((currentPreferences) => ({
                      ...currentPreferences,
                      systemPrompt: event.target.value,
                    }))
                  }
                  placeholder="Set the assistant behavior for this workspace..."
                  rows={3}
                  value={chatPreferences.systemPrompt}
                />
              </label>

              <label className="chat-setting-field chat-setting-field-compact">
                <span>Temperature</span>
                <input
                  max={2}
                  min={0}
                  onChange={(event) =>
                    setChatPreferences((currentPreferences) => ({
                      ...currentPreferences,
                      temperature: event.target.value ? Number(event.target.value) : null,
                    }))
                  }
                  step={0.1}
                  type="number"
                  value={chatPreferences.temperature ?? selectedModel?.defaultTemperature ?? 0.7}
                />
              </label>

              <label className="chat-setting-field chat-setting-field-compact">
                <span>Max output tokens</span>
                <input
                  max={selectedModel?.maxOutputTokens ?? 32768}
                  min={1}
                  onChange={(event) =>
                    setChatPreferences((currentPreferences) => ({
                      ...currentPreferences,
                      maxOutputTokens: event.target.value ? Number(event.target.value) : null,
                    }))
                  }
                  step={1}
                  type="number"
                  value={chatPreferences.maxOutputTokens ?? selectedModel?.defaultMaxOutputTokens ?? 2048}
                />
              </label>
            </section>
          </>
        ) : null}

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
            {!selectedProvider || !selectedModel ? (
              <p>
                AI is not configured yet. <Link to="/settings/ai">Open AI settings</Link>.
              </p>
            ) : null}
          </div>
        )}

        <form
          className="chat-composer"
          onSubmit={(event) => {
            event.preventDefault()
            void handleSendMessage()
          }}
        >
          <label className="sr-only" htmlFor="chat-message">
            Message
          </label>
          <div className="chat-composer-inputs">
            <textarea
              id="chat-message"
              name="message"
              onChange={(event) => setDraftMessage(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter' && !event.shiftKey) {
                  event.preventDefault()
                  void handleSendMessage()
                }
              }}
              placeholder="Ask about code, notes, deployments, or architecture..."
              rows={1}
              value={draftMessage}
            />
          </div>

          <div className="chat-composer-actions">
            {isSelectedSessionSending ? (
              <button
                aria-label="Stop generation"
                className="chat-composer-primary"
                onClick={stopGeneration}
                title="Stop generation"
                type="button"
              >
                Stop
              </button>
            ) : (
              <button
                aria-label="Send message"
                className="chat-composer-primary"
                title="Send message"
                type="submit"
              >
                <span aria-hidden="true">➤</span>
              </button>
            )}
          </div>
        </form>

        {status ? <p className="chat-status">{status}</p> : null}
      </section>
    </section>
  )
}

function loadChatSessions(): ChatSession[] {
  if (typeof window === 'undefined') {
    return []
  }

  const rawValue = window.localStorage.getItem(chatSessionsStorageKey)

  if (!rawValue) {
    return []
  }

  try {
    const parsed = JSON.parse(rawValue) as unknown

    if (!Array.isArray(parsed)) {
      return []
    }

    return parsed
      .map((value) => normalizeSession(value))
      .filter((session): session is ChatSession => session !== null)
      .sort((left, right) => right.updatedAt.localeCompare(left.updatedAt))
  } catch {
    return []
  }
}

function loadChatPreferences(): ChatPreferences {
  if (typeof window === 'undefined') {
    return getDefaultChatPreferences()
  }

  const rawValue = window.localStorage.getItem(chatPreferencesStorageKey)

  if (!rawValue) {
    return getDefaultChatPreferences()
  }

  try {
    const parsed = JSON.parse(rawValue) as Partial<ChatPreferences>

    return {
      maxOutputTokens:
        typeof parsed.maxOutputTokens === 'number' ? parsed.maxOutputTokens : null,
      systemPrompt: typeof parsed.systemPrompt === 'string' ? parsed.systemPrompt : '',
      temperature: typeof parsed.temperature === 'number' ? parsed.temperature : null,
      topP: typeof parsed.topP === 'number' ? parsed.topP : null,
    }
  } catch {
    return getDefaultChatPreferences()
  }
}

function normalizeSession(value: unknown): ChatSession | null {
  if (typeof value !== 'object' || value === null) {
    return null
  }

  const session = value as Record<string, unknown>

  if (
    typeof session.id !== 'string' ||
    typeof session.title !== 'string' ||
    typeof session.updatedAt !== 'string' ||
    !Array.isArray(session.messages)
  ) {
    return null
  }

  return {
    id: session.id,
    messages: session.messages
      .map((item) => normalizeMessage(item))
      .filter((message): message is SessionMessage => message !== null),
    modelId: typeof session.modelId === 'string' ? session.modelId : null,
    providerId: typeof session.providerId === 'string' ? session.providerId : null,
    title: session.title,
    updatedAt: session.updatedAt,
  }
}

function normalizeMessage(value: unknown): SessionMessage | null {
  if (typeof value !== 'object' || value === null) {
    return null
  }

  const message = value as Record<string, unknown>

  if (
    typeof message.id !== 'string' ||
    typeof message.text !== 'string' ||
    (message.role !== 'assistant' && message.role !== 'user')
  ) {
    return null
  }

  return {
    id: message.id,
    role: message.role,
    text: message.text,
  }
}

function summarizeTitle(message: string) {
  const trimmed = message.trim()

  if (!trimmed) {
    return 'New chat'
  }

  return trimmed.length > 36 ? `${trimmed.slice(0, 36)}...` : trimmed
}

function summarizePreview(message: string) {
  const normalized = message.replace(/\s+/g, ' ').trim()

  if (!normalized) {
    return 'No messages yet.'
  }

  return normalized.length > 56 ? `${normalized.slice(0, 56)}...` : normalized
}

function upsertSession(sessions: ChatSession[], session: ChatSession) {
  return [
    session,
    ...sessions.filter((item) => item.id !== session.id),
  ]
}

function formatRelativeTime(value: string) {
  const elapsedMs = Date.now() - new Date(value).getTime()
  const elapsedMinutes = Math.max(1, Math.floor(elapsedMs / 60_000))

  if (elapsedMinutes < 60) {
    return `${elapsedMinutes}m`
  }

  const elapsedHours = Math.floor(elapsedMinutes / 60)

  if (elapsedHours < 24) {
    return `${elapsedHours}h`
  }

  const elapsedDays = Math.floor(elapsedHours / 24)

  return `${elapsedDays}d`
}

function getDefaultChatPreferences(): ChatPreferences {
  return {
    maxOutputTokens: null,
    systemPrompt: '',
    temperature: null,
    topP: null,
  }
}

function toModelOptionValue(providerId: string, modelId: string) {
  return `${providerId}:${modelId}`
}

function loadSelectedSessionId() {
  if (typeof window === 'undefined' || isMobileViewport()) {
    return null
  }

  return window.localStorage.getItem(chatSelectedSessionStorageKey)
}

function isMobileViewport() {
  return globalThis.matchMedia?.('(max-width: 820px)').matches ?? false
}
