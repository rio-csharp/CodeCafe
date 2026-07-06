const EDIT_THREAD_STORAGE_KEY_PREFIX = 'codecafe:ai-edit-thread:'
const CURRENT_VERSION = 1
const TTL_MS = 7 * 24 * 60 * 60 * 1000
const MAX_SIZE_BYTES = 4 * 1024 * 1024

import type { AiEditResponse } from './types'

export interface EditMessage {
  id: string
  role: 'user' | 'assistant' | 'proposal'
  content?: string
  proposal?: AiEditResponse
}

interface PersistedEditThread {
  version: number
  savedAt: string
  messages: EditMessage[]
}

function buildStorageKey(threadKey: string): string {
  return `${EDIT_THREAD_STORAGE_KEY_PREFIX}${threadKey}`
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isPersistedEditThread(value: unknown): value is PersistedEditThread {
  if (!isRecord(value)) return false
  if (value.version !== CURRENT_VERSION) return false
  if (typeof value.savedAt !== 'string') return false
  if (!Array.isArray(value.messages)) return false
  return true
}

export function loadEditThread(threadKey: string): PersistedEditThread | null {
  if (typeof localStorage === 'undefined') return null

  try {
    const raw = localStorage.getItem(buildStorageKey(threadKey))
    if (!raw) return null

    const parsed: unknown = JSON.parse(raw)
    if (!isPersistedEditThread(parsed)) {
      clearEditThread(threadKey)
      return null
    }

    if (Date.now() - new Date(parsed.savedAt).getTime() > TTL_MS) {
      clearEditThread(threadKey)
      return null
    }

    return parsed
  } catch {
    clearEditThread(threadKey)
    return null
  }
}

export function saveEditThread(threadKey: string, messages: readonly EditMessage[]): void {
  if (typeof localStorage === 'undefined') return

  try {
    const payload: PersistedEditThread = {
      version: CURRENT_VERSION,
      savedAt: new Date().toISOString(),
      messages: [...messages],
    }
    const serialized = JSON.stringify(payload)
    if (serialized.length > MAX_SIZE_BYTES) return

    localStorage.setItem(buildStorageKey(threadKey), serialized)
  } catch {
    // Ignore storage errors (quota, private mode, etc.)
  }
}

export function clearEditThread(threadKey: string): void {
  if (typeof localStorage === 'undefined') return

  try {
    localStorage.removeItem(buildStorageKey(threadKey))
  } catch {
    // Ignore storage errors
  }
}
