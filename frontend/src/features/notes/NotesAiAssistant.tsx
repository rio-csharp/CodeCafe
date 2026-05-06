import { useEffect, useMemo, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { streamChatResponse, type ChatMessage } from '../ai/aiClient'
import {
  getDefaultModel,
  getDefaultProvider,
  loadAiSettings,
  type AiProvider,
  type AiProviderModel,
} from '../ai/aiSettingsStore'
import type { NoteContent } from './notesApi'
import { buildNotesAssistantContextPrompt, buildNotesAssistantSystemPrompt, buildNotesDirectoryPrompt } from './notesAiContext'
import type { NoteTreeNode } from './noteTreeBuilder'

const notesAssistantStorageKey = 'codecafe-notes-ai-session'

type AssistantMessage = {
  id: string
  role: 'assistant' | 'user'
  text: string
}

type NotesAssistantSession = {
  contextInjected: boolean
  messages: AssistantMessage[]
  modelId: string | null
  previousResponseId: string | null
  providerId: string | null
}

export function NotesAiAssistant({
  currentNote,
  currentNoteTitle,
  isOpen,
  noteTree,
  onClose,
}: {
  currentNote: NoteContent | null
  currentNoteTitle: string
  isOpen: boolean
  noteTree: NoteTreeNode[]
  onClose: () => void
}) {
  const [draftMessage, setDraftMessage] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [session, setSession] = useState<NotesAssistantSession>(() => loadNotesAssistantSession())
  const [status, setStatus] = useState('')
  const messageThreadRef = useRef<HTMLDivElement | null>(null)
  const abortControllerRef = useRef<AbortController | null>(null)
  const aiSettings = useMemo(() => loadAiSettings(), [])

  const selectedProvider = useMemo(() => {
    if (session.providerId) {
      return aiSettings.providers.find((provider) => provider.id === session.providerId) ?? null
    }

    return getDefaultProvider(aiSettings)
  }, [aiSettings, session.providerId])

  const selectedModel = useMemo(() => {
    if (selectedProvider && session.modelId) {
      return selectedProvider.models.find((model) => model.id === session.modelId) ?? null
    }

    return getDefaultModel(aiSettings)
  }, [aiSettings, selectedProvider, session.modelId])

  const directoryPrompt = useMemo(() => buildNotesDirectoryPrompt(noteTree), [noteTree])

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
      directoryPrompt,
      model,
      provider,
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

    try {
      const result = await streamChatResponse({
        maxOutputTokens: model.defaultMaxOutputTokens,
        messages: requestMessages,
        model,
        onDelta: (delta) => {
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
        previousResponseId:
          usesProviderSideConversation(provider, model) && session.contextInjected
            ? session.previousResponseId
            : null,
        provider,
        signal: controller.signal,
        systemPrompt: buildNotesAssistantSystemPrompt(),
        temperature: model.defaultTemperature,
        topP: model.defaultTopP,
      })

      setSession((currentSession) => ({
        ...currentSession,
        contextInjected: true,
        previousResponseId:
          usesProviderSideConversation(provider, model) && result.responseId
            ? result.responseId
            : currentSession.previousResponseId,
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
      messages: [],
      modelId: selectedModel?.id ?? null,
      previousResponseId: null,
      providerId: selectedProvider?.id ?? null,
    })
  }

  if (!isOpen) {
    return null
  }

  return (
    <>
      <button
        aria-label="Close notes AI assistant"
        className="notes-ai-backdrop"
        onClick={onClose}
        type="button"
      />

      <section className="notes-ai-panel" aria-label="Notes AI assistant">
        <header className="notes-ai-header">
          <div className="notes-ai-title">
            <h3>Ask AI</h3>
            <span>{currentNoteTitle || 'No note selected'}</span>
          </div>

          <div className="notes-ai-actions">
            <button className="icon-button toolbar-icon-button" onClick={clearConversation} type="button" title="Clear conversation">
              <span aria-hidden="true">↺</span>
            </button>
            <button className="icon-button toolbar-icon-button" onClick={onClose} type="button" title="Close assistant">
              <span aria-hidden="true">×</span>
            </button>
          </div>
        </header>

        <div className="notes-ai-thread" ref={messageThreadRef}>
          {session.messages.length > 0 ? (
            session.messages.map((message) => (
              <article className={`message-bubble ${message.role}`} key={message.id}>
                {message.text ? (
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>{message.text}</ReactMarkdown>
                ) : null}
              </article>
            ))
          ) : (
            <div className="empty-thread notes-ai-empty">
              <h3>Ask about this note.</h3>
              <p>The assistant uses the current note and directory structure as context.</p>
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
    </>
  )
}

function buildRequestMessages({
  currentNote,
  currentNoteTitle,
  directoryPrompt,
  model,
  provider,
  session,
  userMessage,
}: {
  currentNote: NoteContent
  currentNoteTitle: string
  directoryPrompt: string
  model: AiProviderModel
  provider: AiProvider
  session: NotesAssistantSession
  userMessage: string
}): ChatMessage[] {
  if (usesProviderSideConversation(provider, model) && session.contextInjected) {
    return [
      {
        role: 'user',
        text: userMessage,
      },
    ]
  }

  const contextMessage = buildNotesAssistantContextPrompt({
    currentNoteContent: currentNote.content,
    currentNotePath: currentNote.path,
    currentNoteTitle,
    directoryTree: directoryPrompt,
  })

  return [
    {
      role: 'user',
      text: contextMessage,
    },
    ...session.messages.map((message) => ({
      role: message.role,
      text: message.text,
    })),
    {
      role: 'user',
      text: userMessage,
    },
  ]
}

function usesProviderSideConversation(provider: AiProvider, model: AiProviderModel) {
  return provider.preferredFormat === 'responses' && !model.supportsStreaming
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
      messages: Array.isArray(parsed.messages)
        ? parsed.messages.filter(isAssistantMessage)
        : [],
      modelId: typeof parsed.modelId === 'string' ? parsed.modelId : null,
      previousResponseId: typeof parsed.previousResponseId === 'string' ? parsed.previousResponseId : null,
      providerId: typeof parsed.providerId === 'string' ? parsed.providerId : null,
    }
  } catch {
    return getEmptyNotesAssistantSession()
  }
}

function getEmptyNotesAssistantSession(): NotesAssistantSession {
  return {
    contextInjected: false,
    messages: [],
    modelId: null,
    previousResponseId: null,
    providerId: null,
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
