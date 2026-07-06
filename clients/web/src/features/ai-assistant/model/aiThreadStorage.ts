import type { Message } from '@ag-ui/core'

export const THREAD_STORAGE_KEY_PREFIX = 'codecafe:ai-thread:'
const CURRENT_VERSION = 1
const TTL_MS = 7 * 24 * 60 * 60 * 1000
const MAX_SIZE_BYTES = 4 * 1024 * 1024

interface PersistedThread {
  version: number
  savedAt: string
  messages: Message[]
}

function buildStorageKey(threadKey: string): string {
  return `${THREAD_STORAGE_KEY_PREFIX}${threadKey}`
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function isPersistedThread(value: unknown): value is PersistedThread {
  if (!isRecord(value)) return false
  if (value.version !== CURRENT_VERSION) return false
  if (typeof value.savedAt !== 'string') return false
  if (!Array.isArray(value.messages)) return false
  return true
}

export function loadThread(threadKey: string): PersistedThread | null {
  if (typeof localStorage === 'undefined') return null

  try {
    const raw = localStorage.getItem(buildStorageKey(threadKey))
    if (!raw) return null

    const parsed: unknown = JSON.parse(raw)
    if (!isPersistedThread(parsed)) {
      clearThread(threadKey)
      return null
    }

    if (Date.now() - new Date(parsed.savedAt).getTime() > TTL_MS) {
      clearThread(threadKey)
      return null
    }

    return parsed
  } catch {
    clearThread(threadKey)
    return null
  }
}

export function saveThread(threadKey: string, messages: readonly Message[]): void {
  if (typeof localStorage === 'undefined') return

  try {
    const payload: PersistedThread = {
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

export function clearThread(threadKey: string): void {
  if (typeof localStorage === 'undefined') return

  try {
    localStorage.removeItem(buildStorageKey(threadKey))
  } catch {
    // Ignore storage errors
  }
}
