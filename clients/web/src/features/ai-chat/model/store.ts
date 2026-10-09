import { randomId } from '@/shared/lib'
import { AiChatHttpError, streamChat } from '../api/streamChat'
import { onSessionCleared } from '@/shared/api'
import { createIndexedDbPersistence } from './persistence'
import type { ChatPersistence } from './persistence'

/** A tool call as the user sees it: a friendly activity, not a technical detail. */
export interface ToolActivity {
  callId: string
  tool: string
  done: boolean
}

export interface ChatMessage {
  id: string
  role: 'user' | 'assistant'
  text: string
  activities: ToolActivity[]
  state: 'streaming' | 'done' | 'error'
}

export interface ChatState {
  messages: ChatMessage[]
  streaming: boolean
  /** Set when a turn failed; shown as a dismissible banner. `status` is the HTTP status for pre-stream failures. */
  error: { code: string; message: string; status?: number } | null
}

/** Tools that change the notebook; after such a turn the reader refetches. */
const MUTATING_TOOLS = new Set(['create_page', 'rename_page', 'move_page', 'apply_block_ops'])

const EMPTY: ChatState = { messages: [], streaming: false, error: null }

/** Internal control flow: the server rejected the history as too long. */
class HistoryTooLong extends Error {}

/**
 * History trimming is deliberately PREFIX-STABLE: nothing is sent but the
 * full conversation (the server caches the prompt prefix — dropping the
 * oldest message every turn would invalidate it on every send). Only when
 * the server reports history_too_long do we drop the oldest half, once, and
 * retry — one cache miss, then the prefix is stable again.
 */

/**
 * One conversation per notebook, living OUTSIDE React: the right panel
 * unmounts inactive tabs, and the chat must survive tab switches and page
 * navigation. Messages are also persisted per notebook (IndexedDB, debounced)
 * so the conversation survives a reload — on this browser only.
 */
export class ChatStore {
  private state: ChatState = EMPTY
  private readonly listeners = new Set<() => void>()
  private abort: AbortController | null = null
  private saveTimer: ReturnType<typeof setTimeout> | null = null
  private generation = 0
  private persistenceQueue: Promise<void> = Promise.resolve()
  private readonly slug: string
  private readonly storageKey: string
  private readonly persistence: ChatPersistence

  constructor(
    slug: string,
    persistence: ChatPersistence = createIndexedDbPersistence(),
    userId: string | null = null,
  ) {
    this.slug = slug
    this.storageKey = chatStorageKey(userId, slug)
    this.persistence = persistence
    void this.hydrate()
  }

  getState = (): ChatState => this.state

  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener)
    return () => {
      this.listeners.delete(listener)
    }
  }

  private setState(next: ChatState, options?: { persist?: boolean }) {
    this.state = next
    for (const listener of this.listeners) {
      listener()
    }
    if (options?.persist !== false) {
      this.scheduleSave()
    }
  }

  /** Restores the persisted conversation, unless the user already started a new one. */
  private async hydrate() {
    const generation = this.generation
    const messages = await this.persistence.load(this.storageKey)
    if (
      generation === this.generation &&
      messages !== null &&
      this.state.messages.length === 0 &&
      !this.state.streaming
    ) {
      this.setState({ ...this.state, messages }, { persist: false })
    }
  }

  private enqueuePersistence(action: () => Promise<void>) {
    this.persistenceQueue = this.persistenceQueue.then(action).catch(() => undefined)
  }

  /**
   * Writes are debounced: streaming patches state many times per second, and
   * only the settled conversation is worth storing.
   */
  private scheduleSave() {
    if (this.saveTimer !== null) {
      clearTimeout(this.saveTimer)
    }
    this.saveTimer = setTimeout(() => {
      this.saveTimer = null
      const messages = this.state.messages
      this.enqueuePersistence(() => this.persistence.save(this.storageKey, messages))
    }, 500)
  }

  private patchAssistant(id: string, patch: (message: ChatMessage) => ChatMessage) {
    this.setState({
      ...this.state,
      messages: this.state.messages.map((message) =>
        message.id === id ? patch(message) : message,
      ),
    })
  }

  async send(text: string, onAiChanged: () => void): Promise<void> {
    if (this.state.streaming || text.trim() === '') {
      return
    }
    const userMessage: ChatMessage = {
      id: randomId(),
      role: 'user',
      text: text.trim(),
      activities: [],
      state: 'done',
    }
    const assistantId = randomId()
    const assistantMessage: ChatMessage = {
      id: assistantId,
      role: 'assistant',
      text: '',
      activities: [],
      state: 'streaming',
    }

    this.setState({
      messages: [...this.state.messages, userMessage, assistantMessage],
      streaming: true,
      error: null,
    })

    const abort = new AbortController()
    this.abort = abort
    const generation = this.generation
    let aiChanged = false

    try {
      await this.streamTurn(assistantId, abort.signal, generation, () => {
        aiChanged = true
      })
      if (generation !== this.generation) {
        return
      }
      // Stream ended without an error frame: the turn is complete.
      this.patchAssistant(assistantId, (message) => ({
        ...message,
        state: message.state === 'streaming' ? 'done' : message.state,
      }))
    } catch (error) {
      if (generation !== this.generation) {
        return
      }
      if (abort.signal.aborted) {
        // The user stopped the reply: keep whatever text arrived.
        this.patchAssistant(assistantId, (message) => ({ ...message, state: 'done' }))
      } else {
        this.patchAssistant(assistantId, (message) => ({ ...message, state: 'error' }))
        this.setState({
          ...this.state,
          error:
            error instanceof AiChatHttpError
              ? { code: error.code, message: error.message, status: error.status }
              : { code: 'ai_stream_failed', message: String(error) },
        })
      }
    } finally {
      if (generation === this.generation) {
        this.abort = null
        this.setState({ ...this.state, streaming: false })
        if (aiChanged) {
          onAiChanged()
        }
      }
    }
  }

  /**
   * One streamed turn over the current history. On history_too_long the
   * oldest half is dropped — ONCE, keeping the prefix stable afterwards —
   * and the turn is retried a single time.
   */
  private async streamTurn(
    assistantId: string,
    signal: AbortSignal,
    generation: number,
    markAiChanged: () => void,
  ): Promise<void> {
    for (let attempt = 0; ; attempt += 1) {
      const history = this.state.messages
        .filter((message) => message.id !== assistantId)
        .map((message) => ({
          role: message.role === 'user' ? ('User' as const) : ('Assistant' as const),
          content: message.text,
        }))
      try {
        await streamChat({
          slug: this.slug,
          messages: history,
          signal,
          onEvent: (event) => {
            if (generation !== this.generation) {
              return
            }
            switch (event.kind) {
              case 'text':
                this.patchAssistant(assistantId, (message) => ({
                  ...message,
                  text: message.text + event.text,
                }))
                break
              case 'tool-call':
                if (MUTATING_TOOLS.has(event.tool)) {
                  markAiChanged()
                }
                this.patchAssistant(assistantId, (message) => ({
                  ...message,
                  activities: [
                    ...message.activities,
                    { callId: event.callId, tool: event.tool, done: false },
                  ],
                }))
                break
              case 'tool-result':
                this.patchAssistant(assistantId, (message) => ({
                  ...message,
                  activities: message.activities.map((activity) =>
                    activity.callId === event.callId ? { ...activity, done: true } : activity,
                  ),
                }))
                break
              case 'error':
                if (event.code === 'history_too_long' && attempt === 0) {
                  throw new HistoryTooLong()
                }
                this.patchAssistant(assistantId, (message) => ({ ...message, state: 'error' }))
                this.setState({
                  ...this.state,
                  error: { code: event.code, message: event.message },
                })
                break
              case 'done':
                break
            }
          },
        })
        return
      } catch (error) {
        if (error instanceof HistoryTooLong) {
          // The retry re-streams from scratch: reset the assistant message so
          // nothing from the rejected attempt survives.
          this.patchAssistant(assistantId, (message) => ({
            ...message,
            text: '',
            activities: [],
          }))
          this.dropOldestHalf()
          continue
        }
        throw error
      }
    }
  }

  /** Drops the oldest half of the conversation (keeping whole turns). */
  private dropOldestHalf() {
    const kept = [...this.state.messages]
    let drop = Math.floor(kept.length / 2)
    // Keep the cut on a user-message boundary so the first kept message is
    // never an orphan assistant reply; always keep the latest turn.
    while (drop > 0 && kept[drop] !== undefined && kept[drop]!.role !== 'user') {
      drop += 1
    }
    drop = Math.min(drop, Math.max(0, kept.length - 2))
    this.setState({ ...this.state, messages: kept.slice(drop) })
  }

  stop() {
    this.abort?.abort()
  }

  /** Stops background work at an auth boundary while keeping this user's settled history. */
  deactivate() {
    this.generation += 1
    this.abort?.abort()
    this.abort = null
    if (this.saveTimer !== null) {
      clearTimeout(this.saveTimer)
      this.saveTimer = null
    }
    const messages = this.state.messages.map((message) =>
      message.state === 'streaming' ? { ...message, state: 'done' as const } : message,
    )
    this.setState({ messages, streaming: false, error: null }, { persist: false })
    this.enqueuePersistence(() => this.persistence.save(this.storageKey, messages))
  }

  clear() {
    this.generation += 1
    this.abort?.abort()
    this.abort = null
    if (this.saveTimer !== null) {
      clearTimeout(this.saveTimer)
      this.saveTimer = null
    }
    this.setState(EMPTY, { persist: false })
    this.enqueuePersistence(() => this.persistence.clear(this.storageKey))
  }
}

const stores = new Map<string, ChatStore>()

onSessionCleared(() => {
  for (const store of stores.values()) {
    store.deactivate()
  }
})

/** The conversation for one notebook slug; created on first use. */
export function chatStorageKey(userId: string | null, slug: string): string {
  return JSON.stringify([userId, slug])
}

export function getChatStore(slug: string, userId: string | null): ChatStore {
  const key = chatStorageKey(userId, slug)
  let store = stores.get(key)
  if (store === undefined) {
    store = new ChatStore(slug, undefined, userId)
    stores.set(key, store)
  }
  return store
}

/** Test hook: drop all conversations. */
export function resetChatStores(): void {
  stores.clear()
}
