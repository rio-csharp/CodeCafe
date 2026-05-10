import { useEffect, useMemo, useRef, useState } from 'react'
import { streamChatResponse } from '../ai/aiClient'
import {
  getEnabledModelOptions,
  isBrowserUnsupportedProvider,
  loadAiSettings,
  resolveModelSelection,
  toModelOptionValue,
  type AiSettings,
} from '../ai/aiSettingsStore'
import { checkBackendHealth } from '../../lib/apiClient'
import { ChatComposer } from './ChatComposer'
import { ChatConversation } from './ChatConversation'
import { ChatSessionList } from './ChatSessionList'
import { ChatSettingsPopover } from './ChatSettingsPopover'
import {
  loadChatPreferences,
  loadChatSessions,
  loadSelectedSessionId,
  saveChatPreferences,
  saveChatSessions,
  saveSelectedSessionId,
} from './chatStorage'
import type { ChatPreferences, ChatSession, SessionMessage } from './chatTypes'
import { summarizeTitle, upsertSession } from './chatUtils'

const healthCheckIntervalMs = 5_000

export function ChatWorkbench() {
  const [aiSettings, setAiSettings] = useState<AiSettings>(() => loadAiSettings())
  const [chatPreferences, setChatPreferences] = useState<ChatPreferences>(() => loadChatPreferences())
  const [draftMessage, setDraftMessage] = useState('')
  const [isBackendHealthy, setIsBackendHealthy] = useState(false)
  const [isSettingsOpen, setIsSettingsOpen] = useState(false)
  const [searchText, setSearchText] = useState('')
  const [sendingSessionIds, setSendingSessionIds] = useState<string[]>([])
  const [selectedModelValue, setSelectedModelValue] = useState<string | null>(() =>
    resolveModelSelection(loadAiSettings(), null),
  )
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(() => loadSelectedSessionId())
  const [sessions, setSessions] = useState<ChatSession[]>(() => loadChatSessions())
  const [status, setStatus] = useState('')
  const abortControllersRef = useRef<Map<string, AbortController>>(new Map())
  const silentAbortSessionIdsRef = useRef<Set<string>>(new Set())

  const enabledModelOptions = useMemo(() => getEnabledModelOptions(aiSettings), [aiSettings])

  const selectedModelOption = useMemo(() => {
    const resolvedValue = resolveModelSelection(aiSettings, selectedModelValue)

    return enabledModelOptions.find((option) => option.value === resolvedValue) ?? null
  }, [aiSettings, enabledModelOptions, selectedModelValue])

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
      setAiSettings(nextSettings)
      setSelectedModelValue((currentModelValue) => resolveModelSelection(nextSettings, currentModelValue))
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
    saveChatSessions(sessions)
  }, [sessions])

  useEffect(() => {
    saveChatPreferences(chatPreferences)
  }, [chatPreferences])

  useEffect(() => {
    saveSelectedSessionId(selectedSessionId)
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

    if (isBrowserUnsupportedProvider(provider)) {
      setStatus('MiniMax is not available in CodeCafe because its browser-side CORS policy blocks our current client-only integration.')
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
    const now = new Date().toISOString()
    const nextSession: ChatSession = {
      id: crypto.randomUUID(),
      messages: [],
      modelId: selectedModel?.id ?? null,
      providerId: selectedProvider?.id ?? null,
      title: 'New chat',
      updatedAt: now,
    }

    setSessions((currentSessions) => upsertSession(currentSessions, nextSession))
    setSelectedSessionId(nextSession.id)
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
      <ChatSessionList
        effectiveSelectedSessionId={effectiveSelectedSessionId}
        filteredSessions={filteredSessions}
        isBackendHealthy={isBackendHealthy}
        onDeleteSession={deleteSession}
        onSearchTextChange={setSearchText}
        onSelectSession={(session) => {
          setSelectedSessionId(session.id)
          if (session.providerId && session.modelId) {
            setSelectedModelValue(toModelOptionValue(session.providerId, session.modelId))
          }
        }}
        onStartNewChat={startNewChat}
        searchText={searchText}
      />

      <section className="chat-console" aria-label="Conversation">
        <ChatConversation
          enabledModelOptions={enabledModelOptions}
          isSettingsOpen={isSettingsOpen}
          onOpenSettingsToggle={() => setIsSettingsOpen((currentValue) => !currentValue)}
          onSelectModelValue={setSelectedModelValue}
          onSelectSessionBack={() => setSelectedSessionId(null)}
          selectedModelOption={selectedModelOption}
          selectedProviderConfigured={Boolean(selectedProvider && selectedModel)}
          selectedSession={selectedSession}
        />

        <ChatSettingsPopover
          chatPreferences={chatPreferences}
          isOpen={isSettingsOpen}
          onClose={() => setIsSettingsOpen(false)}
          onPreferencesChange={setChatPreferences}
          selectedModel={selectedModel}
        />

        <ChatComposer
          draftMessage={draftMessage}
          isSending={isSelectedSessionSending}
          onDraftMessageChange={setDraftMessage}
          onSendMessage={() => void handleSendMessage()}
          onStopGeneration={stopGeneration}
        />

        {status ? <p className="chat-status">{status}</p> : null}
      </section>
    </section>
  )
}
