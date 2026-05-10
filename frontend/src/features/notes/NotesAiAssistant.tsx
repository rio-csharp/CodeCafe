import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { streamChatResponse, type ChatMessage } from '../ai/aiClient'
import { isBrowserUnsupportedProvider } from '../ai/aiSettingsStore'
import { MarkdownContent } from '../../components/MarkdownContent'
import type { NoteContent } from './notesApi'
import {
  buildNotesAssistantContextPrompt,
  buildNotesAssistantSystemPrompt,
} from './notesAiContext'
import { getFabStyle, getPanelStyle } from './notesAiLayout'
import {
  getEmptyNotesAssistantSession,
  loadNotesAssistantSession,
  saveNotesAssistantSession,
} from './notesAiStorage'
import type { AssistantMessage, NotesAssistantSession } from './notesAiTypes'
import { useNotesAiModelSelection } from './useNotesAiModelSelection'
import { useNotesAiPanelPosition } from './useNotesAiPanelPosition'

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
  const {
    enabledModelOptions,
    selectedModel,
    selectedModelOption,
    selectedProvider,
    setSelectedModelValue,
  } = useNotesAiModelSelection()
  const {
    effectivePanelPosition,
    fabPosition,
    handleFabPointerDown,
    handleFabPointerMove,
    handleFabPointerUp,
    handlePanelPointerDown,
    handlePanelPointerMove,
    handlePanelPointerUp,
    isMobile,
  } = useNotesAiPanelPosition()
  const messageThreadRef = useRef<HTMLDivElement | null>(null)
  const abortControllerRef = useRef<AbortController | null>(null)

  useEffect(() => {
    saveNotesAssistantSession(session)
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

  async function handleSend() {
    const provider = selectedProvider
    const model = selectedModel
    const trimmedMessage = draftMessage.trim()

    if (!provider || !model) {
      setStatus('Configure an enabled model in AI settings first.')
      return
    }

    if (isBrowserUnsupportedProvider(provider)) {
      setStatus('MiniMax is not available in CodeCafe because its browser-side CORS policy blocks our current client-only integration.')
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
      ...getEmptyNotesAssistantSession(),
      modelId: selectedModel?.id ?? null,
      providerId: selectedProvider?.id ?? null,
    })
  }

  if (typeof document === 'undefined') {
    return null
  }

  return createPortal((
    <>
      <button
        aria-expanded={isOpen}
        aria-label="Open notes AI assistant"
        className={`notes-ai-fab${isOpen ? ' is-hidden' : ''}`}
        onPointerDown={handleFabPointerDown}
        onPointerMove={handleFabPointerMove}
        onPointerUp={(event) => handleFabPointerUp(event, onOpen)}
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
  ), document.body)
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
