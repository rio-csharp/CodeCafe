import { randomId } from '@/shared/lib'
import type { ChatMessage } from './store'

/**
 * Where conversations live between sessions. IndexedDB on this browser only —
 * nothing is sent to the server, and another browser or device starts empty.
 */
export interface ChatPersistence {
  load(slug: string): Promise<ChatMessage[] | null>
  save(slug: string, messages: ChatMessage[]): Promise<void>
  clear(slug: string): Promise<void>
}

const DB_NAME = 'codecafe'
const STORE_NAME = 'ai-chat'
const DB_VERSION = 1

/** Stored conversations are capped so a long-lived chat cannot grow without bound. */
const MAX_STORED_MESSAGES = 100

interface StoredChat {
  messages: unknown
  updatedAt: string
}

/**
 * IndexedDB-backed persistence. Every failure path resolves to null instead
 * of throwing: storage is a nice-to-have and must never break the chat itself
 * (private windows and locked-down profiles can refuse IndexedDB entirely).
 */
export function createIndexedDbPersistence(): ChatPersistence {
  let dbPromise: Promise<IDBDatabase | null> | null = null

  function openDatabase(): Promise<IDBDatabase | null> {
    dbPromise ??= new Promise((resolve) => {
      if (typeof indexedDB === 'undefined') {
        resolve(null)
        return
      }
      const request = indexedDB.open(DB_NAME, DB_VERSION)
      request.onupgradeneeded = () => {
        request.result.createObjectStore(STORE_NAME)
      }
      request.onsuccess = () => {
        resolve(request.result)
      }
      request.onerror = () => {
        resolve(null)
      }
    })
    return dbPromise
  }

  async function run<T>(
    mode: IDBTransactionMode,
    action: (objectStore: IDBObjectStore) => IDBRequest<T>,
  ): Promise<T | null> {
    const db = await openDatabase()
    if (db === null) {
      return null
    }
    return new Promise((resolve) => {
      const request = action(db.transaction(STORE_NAME, mode).objectStore(STORE_NAME))
      request.onsuccess = () => {
        resolve(request.result)
      }
      request.onerror = () => {
        resolve(null)
      }
    })
  }

  async function clear(slug: string): Promise<void> {
    await run('readwrite', (objectStore) => objectStore.delete(slug))
  }

  return {
    async load(slug) {
      const stored = await run(
        'readonly',
        (objectStore) => objectStore.get(slug) as IDBRequest<StoredChat | undefined>,
      )
      const messages = sanitizeStoredMessages(stored?.messages)
      return messages.length > 0 ? messages : null
    },
    async save(slug, messages) {
      const trimmed = trimToLimit(messages)
      if (trimmed.length === 0) {
        await clear(slug)
        return
      }
      const record: StoredChat = { messages: trimmed, updatedAt: new Date().toISOString() }
      await run('readwrite', (objectStore) => objectStore.put(record, slug))
    },
    clear,
  }
}

/**
 * Stored data is untrusted the moment the schema evolves: drop anything that
 * is not a well-formed message. Nothing restored is ever mid-stream — a
 * reload ends the turn, so streaming text settles as done and unfinished
 * tool activities read as completed rather than pulsing forever.
 */
export function sanitizeStoredMessages(value: unknown): ChatMessage[] {
  if (!Array.isArray(value)) {
    return []
  }
  const messages: ChatMessage[] = []
  for (const entry of value) {
    if (typeof entry !== 'object' || entry === null) {
      continue
    }
    const candidate = entry as Partial<ChatMessage>
    if (
      (candidate.role !== 'user' && candidate.role !== 'assistant') ||
      typeof candidate.text !== 'string'
    ) {
      continue
    }
    messages.push({
      id: typeof candidate.id === 'string' ? candidate.id : randomId(),
      role: candidate.role,
      text: candidate.text,
      activities: (Array.isArray(candidate.activities) ? candidate.activities : [])
        .filter(
          (activity): activity is ChatMessage['activities'][number] =>
            typeof activity === 'object' &&
            activity !== null &&
            typeof activity.callId === 'string' &&
            typeof activity.tool === 'string',
        )
        .map((activity) => ({ ...activity, done: true })),
      state: candidate.state === 'error' ? 'error' : 'done',
    })
  }
  return messages
}

/** Keeps the newest messages, cutting on a user-message boundary like the history trimmer. */
export function trimToLimit(messages: ChatMessage[]): ChatMessage[] {
  if (messages.length <= MAX_STORED_MESSAGES) {
    return messages
  }
  let start = messages.length - MAX_STORED_MESSAGES
  while (start < messages.length && messages[start]!.role !== 'user') {
    start += 1
  }
  return messages.slice(start)
}
