import { useEffect, useMemo, useRef, useState } from 'react'
import { streamChatResponse, type ChatMessage } from '../ai/aiClient'
import { MarkdownContent } from '../../components/MarkdownContent'
import {
  getDefaultModel,
  getDefaultProvider,
  loadAiSettings,
  type AiSettings,
} from '../ai/aiSettingsStore'
import type { NoteContent } from './notesApi'
import {
  buildNotesAssistantContextPrompt,
  buildNotesAssistantSystemPrompt,
} from './notesAiContext'

const notesAssistantStorageKey = 'codecafe-notes-ai-session'
const notesAssistantFabStorageKey = 'codecafe-notes-ai-fab-position'
const defaultFabPosition = { x: 18, y: 18 }
const desktopPanelWidth = 520
const desktopPanelHeight = 680
const mobilePanelWidth = 460
const mobilePanelHeight = 620

type AssistantMessage = {
  id: string
  role: 'assistant' | 'user'
  text: string
}

type NotesAssistantSession = {
  contextInjected: boolean
  contextNotePath: string | null
  messages: AssistantMessage[]
  modelId: string | null
  previousResponseId: string | null
  providerId: string | null
  requestMessages: ChatMessage[]
}

type DragState = {
  didMove: boolean
  initialX: number
  initialY: number
  pointerId: number
  startX: number
  startY: number
}

type PanelPosition = {
  left: number
  top: number
}

export function NotesAiAssistant({
  currentNote,
  currentNoteTitle,
  isOpen,
  onClose,
  onOpen,
}: {
  currentNote: NoteContent | null
  currentNoteTitle: string
  isOpen: boolean
  onClose: () => void
  onOpen: () => void
}) {
  const [draftMessage, setDraftMessage] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [session, setSession] = useState<NotesAssistantSession>(() => loadNotesAssistantSession())
  const [status, setStatus] = useState('')
  const [fabPosition, setFabPosition] = useState(() => loadFabPosition())
  const [panelPosition, setPanelPosition] = useState<PanelPosition | null>(null)
  const [aiSettings, setAiSettings] = useState<AiSettings>(() => loadAiSettings())
  const [selectedModelValue, setSelectedModelValue] = useState<string | null>(() => {
    const settings = loadAiSettings()
    const provider = getDefaultProvider(settings)
    const model = getDefaultModel(settings)

    return provider && model ? toModelOptionValue(provider.id, model.id) : null
  })
  const messageThreadRef = useRef<HTMLDivElement | null>(null)
  const abortControllerRef = useRef<AbortController | null>(null)
  const dragStateRef = useRef<DragState | null>(null)
  const panelDragStateRef = useRef<{
    initialLeft: number
    initialTop: number
    pointerId: number
    startX: number
    startY: number
  } | null>(null)

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
    const provider = getDefaultProvider(aiSettings)
    const model = getDefaultModel(aiSettings)

    return provider && model
      ? enabledModelOptions.find((option) => option.value === toModelOptionValue(provider.id, model.id)) ?? null
      : enabledModelOptions[0] ?? null
  }, [aiSettings, enabledModelOptions])

  const selectedModelOption = useMemo(() => {
    if (!selectedModelValue) {
      return defaultModelOption
    }

    return enabledModelOptions.find((option) => option.value === selectedModelValue) ?? defaultModelOption
  }, [defaultModelOption, enabledModelOptions, selectedModelValue])

  const selectedProvider = selectedModelOption?.provider ?? null
  const selectedModel = selectedModelOption?.model ?? null
  const isMobile = isMobileViewport()
  const effectivePanelPosition = panelPosition ?? getAnchoredPanelPosition(fabPosition, isMobile)

  useEffect(() => {
    window.localStorage.setItem(notesAssistantStorageKey, JSON.stringify(session))
  }, [session])

  useEffect(() => {
    if (!isOpen) {
      return
    }

    if (typeof messageThreadRef.current?.scrollTo === 'function') {
      messageThreadRef.current.scrollTo({
        top: messageThreadRef.current.scrollHeight,
      })
    }

  }, [isOpen, session.messages])

  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort()
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
      setSelectedModelValue((currentValue) => {
        if (currentValue && nextOptions.includes(currentValue)) {
          return currentValue
        }

        const defaultProvider = getDefaultProvider(nextSettings)
        const defaultModel = getDefaultModel(nextSettings)

        return defaultProvider && defaultModel
          ? toModelOptionValue(defaultProvider.id, defaultModel.id)
          : nextOptions[0] ?? null
      })
    }

    window.addEventListener('storage', syncSettings)
    window.addEventListener('focus', syncSettings)

    return () => {
      window.removeEventListener('storage', syncSettings)
      window.removeEventListener('focus', syncSettings)
    }
  }, [])

  async function handleSend() {
    const provider = selectedProvider
    const model = selectedModel
    const trimmedMessage = draftMessage.trim()

    if (!provider || !model) {
      setStatus('Configure an enabled model in AI settings first.')
      return
    }

    if (!provider.baseUrl.trim() || !provider.apiKey.trim()) {
      setStatus('Add a base URL and API key in AI settings first.')
      return
    }

    if (!currentNote || !trimmedMessage || isSending) {
      return
    }

    const assistantMessageId = crypto.randomUUID()
    const userMessage: AssistantMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      text: trimmedMessage,
    }
    const assistantMessage: AssistantMessage = {
      id: assistantMessageId,
      role: 'assistant',
      text: '',
    }
    const nextMessages = [...session.messages, userMessage, assistantMessage]
    const requestMessages = buildRequestMessages({
      currentNote,
      currentNoteTitle,
      session,
      userMessage: trimmedMessage,
    })

    setDraftMessage('')
    setIsSending(true)
    setStatus('')
    setSession((currentSession) => ({
      ...currentSession,
      messages: nextMessages,
      modelId: model.id,
      providerId: provider.id,
    }))

    const controller = new AbortController()
    abortControllerRef.current = controller
    let assistantResponseText = ''

    try {
      const result = await streamChatResponse({
        maxOutputTokens: model.defaultMaxOutputTokens,
        messages: requestMessages,
        model,
        onDelta: (delta) => {
          assistantResponseText = `${assistantResponseText}${delta}`
          setSession((currentSession) => ({
            ...currentSession,
            messages: currentSession.messages.map((message) =>
              message.id === assistantMessageId
                ? {
                    ...message,
                    text: `${message.text}${delta}`,
                  }
                : message,
            ),
          }))
        },
        previousResponseId: null,
        provider,
        signal: controller.signal,
        systemPrompt: buildNotesAssistantSystemPrompt(),
        temperature: model.defaultTemperature,
        topP: model.defaultTopP,
      })

      setSession((currentSession) => ({
        ...currentSession,
        contextInjected: true,
        contextNotePath: currentNote.path,
        messages: currentSession.messages.map((item) =>
          item.id === assistantMessageId
            ? {
                ...item,
                text: assistantResponseText,
              }
            : item,
        ),
        previousResponseId: result.responseId ?? currentSession.previousResponseId,
        requestMessages: [
          ...requestMessages,
          {
            role: 'assistant',
            text: assistantResponseText,
          },
        ],
      }))
    } catch (error) {
      const message =
        error instanceof Error && error.name === 'AbortError'
          ? 'Generation stopped.'
          : error instanceof Error
            ? error.message
            : 'Request failed.'

      setSession((currentSession) => ({
        ...currentSession,
        messages: currentSession.messages.map((item) =>
          item.id === assistantMessageId
            ? {
                ...item,
                text: item.text.length > 0 ? item.text : message,
              }
            : item,
        ),
        requestMessages: [
          ...requestMessages,
          {
            role: 'assistant',
            text: assistantResponseText || message,
          },
        ],
      }))
      setStatus(message)
    } finally {
      abortControllerRef.current = null
      setIsSending(false)
    }
  }

  function clearConversation() {
    abortControllerRef.current?.abort()
    abortControllerRef.current = null
    setStatus('')
    setSession({
      contextInjected: false,
      contextNotePath: null,
      messages: [],
      modelId: selectedModel?.id ?? null,
      previousResponseId: null,
      providerId: selectedProvider?.id ?? null,
      requestMessages: [],
    })
  }

  function handleFabPointerDown(event: React.PointerEvent<HTMLButtonElement>) {
    dragStateRef.current = {
      didMove: false,
      initialX: fabPosition.x,
      initialY: fabPosition.y,
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
    }

    event.currentTarget.setPointerCapture?.(event.pointerId)
  }

  function handleFabPointerMove(event: React.PointerEvent<HTMLButtonElement>) {
    const dragState = dragStateRef.current

    if (!dragState || dragState.pointerId !== event.pointerId) {
      return
    }

    const deltaX = event.clientX - dragState.startX
    const deltaY = event.clientY - dragState.startY

    if (Math.abs(deltaX) > 3 || Math.abs(deltaY) > 3) {
      dragState.didMove = true
    }

    const nextX = Math.max(12, dragState.initialX - deltaX)
    const nextY = Math.max(12, dragState.initialY - deltaY)
    setFabPosition({ x: nextX, y: nextY })
  }

  function handleFabPointerUp(event: React.PointerEvent<HTMLButtonElement>) {
    const dragState = dragStateRef.current

    if (!dragState || dragState.pointerId !== event.pointerId) {
      return
    }

    saveFabPosition(fabPosition)

    if (!dragState.didMove) {
      onOpen()
    }

    dragStateRef.current = null
    event.currentTarget.releasePointerCapture?.(event.pointerId)
  }

  function handlePanelPointerDown(event: React.PointerEvent<HTMLElement>) {
    if (isMobile) {
      return
    }

    const target = event.target

    if (
      target instanceof HTMLElement &&
      target.closest('button, select, input, textarea, a')
    ) {
      return
    }

    const basePosition = effectivePanelPosition

    panelDragStateRef.current = {
      initialLeft: basePosition.left,
      initialTop: basePosition.top,
      pointerId: event.pointerId,
      startX: event.clientX,
      startY: event.clientY,
    }

    event.currentTarget.setPointerCapture?.(event.pointerId)
  }

  function handlePanelPointerMove(event: React.PointerEvent<HTMLElement>) {
    const dragState = panelDragStateRef.current

    if (!dragState || dragState.pointerId !== event.pointerId || isMobile) {
      return
    }

    const nextLeft = dragState.initialLeft + (event.clientX - dragState.startX)
    const nextTop = dragState.initialTop + (event.clientY - dragState.startY)
    setPanelPosition(clampPanelPosition({
      left: nextLeft,
      top: nextTop,
    }, false))
  }

  function handlePanelPointerUp(event: React.PointerEvent<HTMLElement>) {
    if (panelDragStateRef.current?.pointerId === event.pointerId) {
      panelDragStateRef.current = null
      event.currentTarget.releasePointerCapture?.(event.pointerId)
    }
  }

  return (
    <>
      <button
        aria-expanded={isOpen}
        aria-label="Open notes AI assistant"
        className={`notes-ai-fab${isOpen ? ' is-hidden' : ''}`}
        onPointerDown={handleFabPointerDown}
        onPointerMove={handleFabPointerMove}
        onPointerUp={handleFabPointerUp}
        style={getFabStyle(fabPosition)}
        type="button"
      >
        AI
      </button>

      {isOpen ? (
        <section
          aria-label="Notes AI assistant"
          className="notes-ai-panel"
          style={getPanelStyle(effectivePanelPosition, isMobile)}
        >
          <header
            className="notes-ai-header"
            onPointerDown={handlePanelPointerDown}
            onPointerMove={handlePanelPointerMove}
            onPointerUp={handlePanelPointerUp}
          >
            <div className="notes-ai-title">
              <h3>Ask AI</h3>
            </div>

            <div className="notes-ai-toolbar">
              <select
                aria-label="Notes AI model"
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

              <div className="notes-ai-actions">
                <button className="icon-button toolbar-icon-button" onClick={clearConversation} type="button" title="Clear conversation">
                  <span aria-hidden="true">↺</span>
                </button>
                <button className="icon-button toolbar-icon-button" onClick={onClose} type="button" title="Close assistant">
                  <span aria-hidden="true">×</span>
                </button>
              </div>
            </div>
          </header>

          <div className="notes-ai-thread" ref={messageThreadRef}>
            {session.messages.length > 0 ? (
              session.messages.map((message) => (
                <article className={`message-bubble notes-ai-message-bubble ${message.role}`} key={message.id}>
                  {message.text ? (
                    <MarkdownContent>{message.text}</MarkdownContent>
                  ) : null}
                </article>
              ))
            ) : (
              <div className="empty-thread notes-ai-empty">
                <h3>Ask about this note.</h3>
                <p>{currentNoteTitle || 'Use the current note as context.'}</p>
              </div>
            )}
          </div>

          <form
            className="chat-composer notes-ai-composer"
            onSubmit={(event) => {
              event.preventDefault()
              void handleSend()
            }}
          >
            <div className="chat-composer-inputs">
              <textarea
                aria-label="Ask AI about this note"
                onChange={(event) => setDraftMessage(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault()
                    void handleSend()
                  }
                }}
                placeholder="Ask about the current note, related topics, or summarize a section..."
                rows={1}
                value={draftMessage}
              />
            </div>

            <div className="chat-composer-actions">
              {isSending ? (
                <button
                  aria-label="Stop notes AI generation"
                  className="chat-composer-primary"
                  onClick={() => abortControllerRef.current?.abort()}
                  type="button"
                >
                  Stop
                </button>
              ) : (
                <button aria-label="Send notes AI message" className="chat-composer-primary" type="submit">
                  <span aria-hidden="true">➤</span>
                </button>
              )}
            </div>
          </form>

          {status ? <p className="chat-status">{status}</p> : null}
        </section>
      ) : null}
    </>
  )
}

function buildRequestMessages({
  currentNote,
  currentNoteTitle,
  session,
  userMessage,
}: {
  currentNote: NoteContent
  currentNoteTitle: string
  session: NotesAssistantSession
  userMessage: string
}): ChatMessage[] {
  const shouldInjectContext =
    !session.contextInjected ||
    session.contextNotePath !== currentNote.path ||
    session.requestMessages.length === 0

  const baseMessages = shouldInjectContext
    ? [{
        role: 'user' as const,
        text: buildNotesAssistantContextPrompt({
          currentNoteContent: currentNote.content,
          currentNoteTitle,
        }),
      }]
    : session.requestMessages

  return [...baseMessages, {
    role: 'user',
    text: userMessage,
  }]
}

function loadNotesAssistantSession(): NotesAssistantSession {
  if (typeof window === 'undefined') {
    return getEmptyNotesAssistantSession()
  }

  const rawValue = window.localStorage.getItem(notesAssistantStorageKey)

  if (!rawValue) {
    return getEmptyNotesAssistantSession()
  }

  try {
    const parsed = JSON.parse(rawValue) as Partial<NotesAssistantSession>

    return {
      contextInjected: parsed.contextInjected === true,
      contextNotePath: typeof parsed.contextNotePath === 'string' ? parsed.contextNotePath : null,
      messages: Array.isArray(parsed.messages)
        ? parsed.messages.filter(isAssistantMessage)
        : [],
      modelId: typeof parsed.modelId === 'string' ? parsed.modelId : null,
      previousResponseId: typeof parsed.previousResponseId === 'string' ? parsed.previousResponseId : null,
      providerId: typeof parsed.providerId === 'string' ? parsed.providerId : null,
      requestMessages: Array.isArray(parsed.requestMessages)
        ? parsed.requestMessages.filter(isChatMessage)
        : [],
    }
  } catch {
    return getEmptyNotesAssistantSession()
  }
}

function getEmptyNotesAssistantSession(): NotesAssistantSession {
  return {
    contextInjected: false,
    contextNotePath: null,
    messages: [],
    modelId: null,
    previousResponseId: null,
    providerId: null,
    requestMessages: [],
  }
}

function isAssistantMessage(value: unknown): value is AssistantMessage {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  const message = value as Record<string, unknown>

  return (
    typeof message.id === 'string' &&
    typeof message.text === 'string' &&
    (message.role === 'assistant' || message.role === 'user')
  )
}

function isChatMessage(value: unknown): value is ChatMessage {
  if (typeof value !== 'object' || value === null) {
    return false
  }

  const message = value as Record<string, unknown>

  return (
    typeof message.text === 'string' &&
    (message.role === 'assistant' || message.role === 'system' || message.role === 'user')
  )
}

function toModelOptionValue(providerId: string, modelId: string) {
  return `${providerId}:${modelId}`
}

function getFabStyle(position: { x: number; y: number }) {
  return {
    bottom: `${position.y}px`,
    right: `${position.x}px`,
  }
}

function getPanelStyle(position: PanelPosition, isMobile: boolean) {
  return {
    left: `${position.left}px`,
    top: `${position.top}px`,
    ...(isMobile
      ? {
          height: `min(70vh, ${mobilePanelHeight}px)`,
          width: `min(calc(100vw - 24px), ${mobilePanelWidth}px)`,
        }
      : {
          height: `min(calc(100vh - 126px), ${desktopPanelHeight}px)`,
          width: `min(calc(100vw - 36px), ${desktopPanelWidth}px)`,
        }),
  }
}

function getAnchoredPanelPosition(fabPosition: { x: number; y: number }, isMobile: boolean) {
  const viewportWidth = typeof window === 'undefined' ? 1440 : window.innerWidth
  const viewportHeight = typeof window === 'undefined' ? 900 : window.innerHeight
  const panelWidth = Math.min(viewportWidth - (isMobile ? 24 : 36), isMobile ? mobilePanelWidth : desktopPanelWidth)
  const panelHeight = Math.min(viewportHeight - (isMobile ? 96 : 126), isMobile ? mobilePanelHeight : desktopPanelHeight)
  const left = viewportWidth - fabPosition.x - panelWidth
  const top = viewportHeight - fabPosition.y - panelHeight - 64

  return clampPanelPosition({
    left,
    top,
  }, isMobile)
}

function clampPanelPosition(position: PanelPosition, isMobile: boolean) {
  const viewportWidth = typeof window === 'undefined' ? 1440 : window.innerWidth
  const viewportHeight = typeof window === 'undefined' ? 900 : window.innerHeight
  const panelWidth = Math.min(viewportWidth - (isMobile ? 24 : 36), isMobile ? mobilePanelWidth : desktopPanelWidth)
  const panelHeight = Math.min(viewportHeight - (isMobile ? 96 : 126), isMobile ? mobilePanelHeight : desktopPanelHeight)
  const margin = isMobile ? 12 : 18

  return {
    left: Math.min(Math.max(margin, position.left), viewportWidth - panelWidth - margin),
    top: Math.min(Math.max(margin, position.top), viewportHeight - panelHeight - margin),
  }
}

function loadFabPosition() {
  if (typeof window === 'undefined') {
    return defaultFabPosition
  }

  const rawValue = window.localStorage.getItem(notesAssistantFabStorageKey)

  if (!rawValue) {
    return defaultFabPosition
  }

  try {
    const parsed = JSON.parse(rawValue) as Partial<{ x: number; y: number }>

    return {
      x: typeof parsed.x === 'number' ? parsed.x : defaultFabPosition.x,
      y: typeof parsed.y === 'number' ? parsed.y : defaultFabPosition.y,
    }
  } catch {
    return defaultFabPosition
  }
}

function saveFabPosition(position: { x: number; y: number }) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(notesAssistantFabStorageKey, JSON.stringify(position))
}

function isMobileViewport() {
  return globalThis.matchMedia?.('(max-width: 820px)').matches ?? false
}
