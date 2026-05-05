import { useEffect, useMemo, useRef, useState } from 'react'
import type { ChangeEvent } from 'react'
import { Link } from 'react-router-dom'
import { streamChatResponse, type ChatAttachment, testProviderConnection } from '../ai/aiClient'
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

type ChatPreferences = {
  maxOutputTokens: number | null
  systemPrompt: string
  temperature: number | null
  topP: number | null
}

type SessionMessage = {
  attachments: ChatAttachment[]
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
  const [draftAttachments, setDraftAttachments] = useState<ChatAttachment[]>([])
  const [isBackendHealthy, setIsBackendHealthy] = useState(false)
  const [isTestingConnection, setIsTestingConnection] = useState(false)
  const [isSending, setIsSending] = useState(false)
  const [searchText, setSearchText] = useState('')
  const [selectedModelId, setSelectedModelId] = useState<string | null>(
    () => getDefaultModel(loadAiSettings())?.id ?? null,
  )
  const [selectedProviderId, setSelectedProviderId] = useState<string | null>(
    () => getDefaultProvider(loadAiSettings())?.id ?? null,
  )
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null)
  const [sessions, setSessions] = useState<ChatSession[]>(() => loadChatSessions())
  const [status, setStatus] = useState('')
  const abortControllerRef = useRef<AbortController | null>(null)
  const fileInputRef = useRef<HTMLInputElement | null>(null)

  const selectedProvider = useMemo(() => {
    if (!selectedProviderId) {
      return getDefaultProvider(aiSettings)
    }

    return aiSettings.providers.find((provider) => provider.id === selectedProviderId) ?? null
  }, [aiSettings, selectedProviderId])

  const enabledProviders = useMemo(
    () => aiSettings.providers.filter((provider) => provider.enabled),
    [aiSettings.providers],
  )

  const enabledModels = useMemo(
    () => selectedProvider?.models.filter((model) => model.enabled) ?? [],
    [selectedProvider],
  )

  const selectedModel = useMemo(() => {
    if (!selectedModelId) {
      return getDefaultModel(aiSettings)
    }

    return selectedProvider?.models.find((model) => model.id === selectedModelId) ?? null
  }, [aiSettings, selectedModelId, selectedProvider])

  const selectedSession = sessions.find((session) => session.id === selectedSessionId) ?? null

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
      const defaultProvider = getDefaultProvider(nextSettings)
      const defaultModel = getDefaultModel(nextSettings)

      setAiSettings(nextSettings)

      setSelectedProviderId((currentProviderId) => {
        if (
          currentProviderId &&
          nextSettings.providers.some((provider) => provider.id === currentProviderId && provider.enabled)
        ) {
          return currentProviderId
        }

        return defaultProvider?.id ?? null
      })

      setSelectedModelId((currentModelId) => {
        const provider =
          nextSettings.providers.find((item) => item.id === (selectedProviderId ?? defaultProvider?.id)) ??
          defaultProvider ??
          null

        if (
          currentModelId &&
          provider?.models.some((model) => model.id === currentModelId && model.enabled)
        ) {
          return currentModelId
        }

        return defaultModel?.id ?? provider?.models.find((model) => model.enabled)?.id ?? null
      })
    }

    syncSettings()
    window.addEventListener('storage', syncSettings)
    window.addEventListener('focus', syncSettings)

    return () => {
      window.removeEventListener('storage', syncSettings)
      window.removeEventListener('focus', syncSettings)
    }
  }, [selectedProviderId])

  useEffect(() => {
    window.localStorage.setItem(chatSessionsStorageKey, JSON.stringify(sessions))
  }, [sessions])

  useEffect(() => {
    window.localStorage.setItem(chatPreferencesStorageKey, JSON.stringify(chatPreferences))
  }, [chatPreferences])

  useEffect(() => {
    document.body.classList.toggle('chat-session-open', Boolean(selectedSession))

    return () => {
      document.body.classList.remove('chat-session-open')
    }
  }, [selectedSession])

  async function handleSendMessage() {
    if (isSending) {
      return
    }

    const provider = selectedProvider
    const model = selectedModel
    const trimmedMessage = draftMessage.trim()
    const effectiveMaxOutputTokens = chatPreferences.maxOutputTokens ?? model?.defaultMaxOutputTokens ?? 2048
    const effectiveTemperature = chatPreferences.temperature ?? model?.defaultTemperature ?? 0.7
    const effectiveTopP = chatPreferences.topP ?? model?.defaultTopP ?? 1

    if (!provider || !model) {
      setStatus('Configure an enabled provider and model in AI settings first.')
      return
    }

    if (!provider.baseUrl.trim() || !provider.apiKey.trim()) {
      setStatus('Add a base URL and API key before sending a message.')
      return
    }

    if (!trimmedMessage && draftAttachments.length === 0) {
      return
    }

    const now = new Date().toISOString()
    const userMessage: SessionMessage = {
      attachments: [...draftAttachments],
      id: crypto.randomUUID(),
      role: 'user',
      text: trimmedMessage,
    }
    const assistantMessage: SessionMessage = {
      attachments: [],
      id: crypto.randomUUID(),
      role: 'assistant',
      text: '',
    }
    const targetSessionId = selectedSession?.id ?? crypto.randomUUID()
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
    setDraftAttachments([])
    setStatus('')
    setIsSending(true)

    const controller = new AbortController()
    abortControllerRef.current = controller

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
      setStatus(message)
    } finally {
      abortControllerRef.current = null
      setIsSending(false)
    }
  }

  async function handleAttachmentChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]

    if (!file) {
      return
    }

    const attachment = await fileToAttachment(file)

    setDraftAttachments((currentAttachments) => [...currentAttachments, attachment])
    event.target.value = ''
  }

  function startNewChat() {
    setSelectedSessionId(null)
    setDraftMessage('')
    setDraftAttachments([])
    setStatus('')
  }

  function stopGeneration() {
    abortControllerRef.current?.abort()
  }

  async function handleTestConnection() {
    if (!selectedProvider) {
      setStatus('Choose a provider first.')
      return
    }

    if (!selectedProvider.baseUrl.trim() || !selectedProvider.apiKey.trim()) {
      setStatus('Add a base URL and API key before testing the connection.')
      return
    }

    setIsTestingConnection(true)
    setStatus('Testing connection...')

    const result = await testProviderConnection({
      model: selectedModel,
      provider: selectedProvider,
    })

    setStatus(result.message)
    setIsTestingConnection(false)
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
              <button
                aria-current={session.id === selectedSessionId ? 'true' : undefined}
                className="session-item"
                key={session.id}
                onClick={() => {
                  setSelectedSessionId(session.id)
                  setSelectedProviderId(session.providerId)
                  setSelectedModelId(session.modelId)
                }}
                type="button"
              >
                <span>
                  <strong>{session.title}</strong>
                  <small>{session.messages.at(-1)?.text || 'No messages yet.'}</small>
                </span>
                <time>{formatRelativeTime(session.updatedAt)}</time>
              </button>
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
                aria-label="Provider"
                className="chat-select"
                onChange={(event) => {
                  const nextProviderId = event.target.value || null
                  const nextProvider =
                    enabledProviders.find((provider) => provider.id === nextProviderId) ?? null
                  const nextModel =
                    nextProvider?.models.find((model) => model.id === aiSettings.defaultModelId && model.enabled) ??
                    nextProvider?.models.find((model) => model.enabled) ??
                    null

                  setSelectedProviderId(nextProviderId)
                  setSelectedModelId(nextModel?.id ?? null)
                }}
                value={selectedProvider?.id ?? ''}
              >
                {enabledProviders.length > 0 ? (
                  enabledProviders.map((provider) => (
                    <option key={provider.id} value={provider.id}>
                      {provider.name}
                    </option>
                  ))
                ) : (
                  <option value="">No provider</option>
                )}
              </select>

              <select
                aria-label="Model"
                className="chat-select"
                onChange={(event) => setSelectedModelId(event.target.value || null)}
                value={selectedModel?.id ?? ''}
              >
                {enabledModels.length > 0 ? (
                  enabledModels.map((model) => (
                    <option key={model.id} value={model.id}>
                      {model.name}
                    </option>
                  ))
                ) : (
                  <option value="">No model</option>
                )}
              </select>

              <button
                className="icon-button"
                disabled={isTestingConnection}
                onClick={() => void handleTestConnection()}
                type="button"
              >
                {isTestingConnection ? 'Testing' : 'Test'}
              </button>
            </div>
          </div>
        </header>

        <section className="chat-settings-bar" aria-label="Chat controls">
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
              rows={2}
              value={chatPreferences.systemPrompt}
            />
          </label>

          <label className="chat-setting-field">
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

          <label className="chat-setting-field">
            <span>Top-p</span>
            <input
              max={1}
              min={0}
              onChange={(event) =>
                setChatPreferences((currentPreferences) => ({
                  ...currentPreferences,
                  topP: event.target.value ? Number(event.target.value) : null,
                }))
              }
              step={0.05}
              type="number"
              value={chatPreferences.topP ?? selectedModel?.defaultTopP ?? 1}
            />
          </label>

          <label className="chat-setting-field">
            <span>Max tokens</span>
            <input
              max={32768}
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

        {selectedSession ? (
          <div className="message-thread" aria-label={`${selectedSession.title} messages`}>
            {selectedSession.messages.map((message) => (
              <article className={`message-bubble ${message.role}`} key={message.id}>
                {message.text ? <p>{message.text}</p> : null}
                {message.attachments.length > 0 ? (
                  <div className="message-attachments">
                    {message.attachments.map((attachment) => (
                      <img
                        alt={attachment.name}
                        className="message-attachment-preview"
                        key={attachment.id}
                        src={attachment.dataUrl}
                      />
                    ))}
                  </div>
                ) : null}
              </article>
            ))}
          </div>
        ) : (
          <div className="empty-thread" aria-label="Empty conversation">
            <h3>Start a chat.</h3>
            <p>
              Pick a provider and model, then ask about code, notes, architecture, or anything
              else you want the workspace to help with.
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
            {draftAttachments.length > 0 ? (
              <div className="draft-attachment-strip">
                {draftAttachments.map((attachment) => (
                  <button
                    className="draft-attachment-chip"
                    key={attachment.id}
                    onClick={() =>
                      setDraftAttachments((currentAttachments) =>
                        currentAttachments.filter((item) => item.id !== attachment.id),
                      )
                    }
                    type="button"
                  >
                    {attachment.name}
                  </button>
                ))}
              </div>
            ) : null}
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

          <input
            accept="image/*"
            className="sr-only"
            onChange={(event) => void handleAttachmentChange(event)}
            ref={fileInputRef}
            type="file"
          />

          <button
            disabled={!selectedModel?.supportsImages}
            onClick={() => fileInputRef.current?.click()}
            type="button"
          >
            Image
          </button>

          {isSending ? (
            <button onClick={stopGeneration} type="button">
              Stop
            </button>
          ) : (
            <button type="submit">Send</button>
          )}
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
    attachments: Array.isArray(message.attachments)
      ? message.attachments.filter(isAttachment)
      : [],
    id: message.id,
    role: message.role,
    text: message.text,
  }
}

function isAttachment(value: unknown): value is ChatAttachment {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  const attachment = value as Record<string, unknown>

  return (
    typeof attachment.id === 'string' &&
    typeof attachment.dataUrl === 'string' &&
    typeof attachment.mediaType === 'string' &&
    typeof attachment.name === 'string'
  )
}

function summarizeTitle(message: string) {
  const trimmed = message.trim()

  if (!trimmed) {
    return 'New chat'
  }

  return trimmed.length > 36 ? `${trimmed.slice(0, 36)}...` : trimmed
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

function fileToAttachment(file: File) {
  return new Promise<ChatAttachment>((resolve, reject) => {
    const reader = new FileReader()

    reader.onload = () => {
      if (typeof reader.result !== 'string') {
        reject(new Error('Unable to read file.'))
        return
      }

      resolve({
        dataUrl: reader.result,
        id: crypto.randomUUID(),
        mediaType: file.type,
        name: file.name,
      })
    }

    reader.onerror = () => {
      reject(new Error('Unable to read file.'))
    }

    reader.readAsDataURL(file)
  })
}
