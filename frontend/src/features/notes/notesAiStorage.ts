import type { ChatMessage } from '../ai/aiClient'
import type { AssistantMessage, NotesAssistantSession } from './notesAiTypes'

const notesAssistantStorageKey = 'codecafe-notes-ai-session'
const notesAssistantFabStorageKey = 'codecafe-notes-ai-fab-position'

export const defaultFabPosition = { x: 18, y: 18 }

export function loadNotesAssistantSession(): NotesAssistantSession {
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

export function saveNotesAssistantSession(session: NotesAssistantSession) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(notesAssistantStorageKey, JSON.stringify(session))
}

export function loadFabPosition() {
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
      x:
        typeof parsed.x === 'number' && Number.isFinite(parsed.x)
          ? parsed.x
          : defaultFabPosition.x,
      y:
        typeof parsed.y === 'number' && Number.isFinite(parsed.y)
          ? parsed.y
          : defaultFabPosition.y,
    }
  } catch {
    return defaultFabPosition
  }
}

export function saveFabPosition(position: { x: number; y: number }) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(notesAssistantFabStorageKey, JSON.stringify(position))
}

export function getEmptyNotesAssistantSession(): NotesAssistantSession {
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
