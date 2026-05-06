import type { ChatPreferences, ChatSession, SessionMessage } from './chatTypes'
import { isMobileViewport } from './chatUtils'

const chatSessionsStorageKey = 'codecafe-chat-sessions'
const chatPreferencesStorageKey = 'codecafe-chat-preferences'
const chatSelectedSessionStorageKey = 'codecafe-chat-selected-session'

export function loadChatSessions(): ChatSession[] {
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

export function saveChatSessions(sessions: ChatSession[]) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(chatSessionsStorageKey, JSON.stringify(sessions))
}

export function loadChatPreferences(): ChatPreferences {
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

export function saveChatPreferences(preferences: ChatPreferences) {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(chatPreferencesStorageKey, JSON.stringify(preferences))
}

export function loadSelectedSessionId() {
  if (typeof window === 'undefined' || isMobileViewport()) {
    return null
  }

  return window.localStorage.getItem(chatSelectedSessionStorageKey)
}

export function saveSelectedSessionId(selectedSessionId: string | null) {
  if (typeof window === 'undefined') {
    return
  }

  if (isMobileViewport() || !selectedSessionId) {
    window.localStorage.removeItem(chatSelectedSessionStorageKey)
    return
  }

  window.localStorage.setItem(chatSelectedSessionStorageKey, selectedSessionId)
}

function getDefaultChatPreferences(): ChatPreferences {
  return {
    maxOutputTokens: null,
    systemPrompt: '',
    temperature: null,
    topP: null,
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
