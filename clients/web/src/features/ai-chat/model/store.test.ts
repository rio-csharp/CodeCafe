import { beforeEach, describe, expect, it, vi } from 'vitest'
import { streamChat } from '../api/streamChat'
import type { StreamChatParams } from '../api/streamChat'
import { ChatStore, chatStorageKey, getChatStore, resetChatStores } from './store'
import type { ChatMessage } from './store'
import type { ChatPersistence } from './persistence'
import { clearSession } from '@/shared/api'

vi.mock('../api/streamChat', () => {
  class AiChatHttpError extends Error {
    readonly status: number
    readonly code: string

    constructor(status: number, code: string, message: string) {
      super(message)
      this.status = status
      this.code = code
    }
  }
  return { streamChat: vi.fn(), AiChatHttpError }
})

/** Resolves after running the given events through the client's onEvent. */
function replyWith(events: Record<string, unknown>[]) {
  vi.mocked(streamChat).mockImplementation(async ({ onEvent }: StreamChatParams) => {
    for (const event of events) {
      onEvent(event as never)
    }
  })
}

describe('ChatStore', () => {
  beforeEach(() => {
    resetChatStores()
    vi.mocked(streamChat).mockReset()
  })

  it('streams a turn: user message, assistant text, done state', async () => {
    replyWith([
      { kind: 'text', text: 'Hello' },
      { kind: 'text', text: ' there' },
      { kind: 'done' },
    ])
    const store = new ChatStore('s')

    await store.send('hi', vi.fn())

    const { messages, streaming } = store.getState()
    expect(streaming).toBe(false)
    expect(messages).toHaveLength(2)
    expect(messages[0]).toMatchObject({ role: 'user', text: 'hi', state: 'done' })
    expect(messages[1]).toMatchObject({ role: 'assistant', text: 'Hello there', state: 'done' })
  })

  it('sends the FULL history every turn so the server-side prefix cache stays warm', async () => {
    replyWith([{ kind: 'done' }])
    const store = new ChatStore('s')

    await store.send('first', vi.fn())
    await store.send('second', vi.fn())

    const secondCall = vi.mocked(streamChat).mock.calls[1]![0]
    expect(secondCall.messages).toEqual([
      { role: 'User', content: 'first' },
      { role: 'Assistant', content: '' },
      { role: 'User', content: 'second' },
    ])
  })

  it('records tool calls as activities and flags mutating turns for a refetch', async () => {
    replyWith([
      { kind: 'tool-call', tool: 'get_page', callId: 'c1' },
      { kind: 'tool-result', callId: 'c1' },
      { kind: 'tool-call', tool: 'apply_block_ops', callId: 'c2' },
      { kind: 'tool-result', callId: 'c2' },
      { kind: 'done' },
    ])
    const onAiChanged = vi.fn()
    const store = new ChatStore('s')

    await store.send('edit it', onAiChanged)

    const assistant = store.getState().messages[1]!
    expect(assistant.activities).toEqual([
      { callId: 'c1', tool: 'get_page', done: true },
      { callId: 'c2', tool: 'apply_block_ops', done: true },
    ])
    expect(onAiChanged).toHaveBeenCalledOnce()
  })

  it('does not refetch after a read-only turn', async () => {
    replyWith([
      { kind: 'tool-call', tool: 'search_pages', callId: 'c1' },
      { kind: 'tool-result', callId: 'c1' },
      { kind: 'done' },
    ])
    const onAiChanged = vi.fn()
    await new ChatStore('s').send('find something', onAiChanged)

    expect(onAiChanged).not.toHaveBeenCalled()
  })

  it('drops the oldest half and retries ONCE on history_too_long', async () => {
    replyWith([{ kind: 'done' }])
    const store = new ChatStore('s')
    await store.send('zero', vi.fn())
    await store.send('one', vi.fn())
    await store.send('two', vi.fn())

    vi.mocked(streamChat)
      .mockImplementationOnce(async ({ onEvent }: StreamChatParams) => {
        onEvent({ kind: 'error', code: 'history_too_long', message: 'too long' } as never)
      })
      .mockImplementationOnce(async ({ onEvent }: StreamChatParams) => {
        onEvent({ kind: 'text', text: 'ok' } as never)
      })

    await store.send('three', vi.fn())

    expect(streamChat).toHaveBeenCalledTimes(5)
    const retriedCall = vi.mocked(streamChat).mock.calls[4]![0]
    // The oldest half is gone; the retried history starts at a user boundary.
    expect(retriedCall.messages).toEqual([
      { role: 'User', content: 'two' },
      { role: 'Assistant', content: '' },
      { role: 'User', content: 'three' },
    ])
    expect(store.getState().messages[store.getState().messages.length - 1]).toMatchObject({
      text: 'ok',
      state: 'done',
    })
  })

  it('surfaces a stream error without retrying when it is not history_too_long', async () => {
    replyWith([{ kind: 'error', code: 'ai_provider_failed', message: 'boom' }])
    const store = new ChatStore('s')

    await store.send('hi', vi.fn())

    expect(store.getState().error).toMatchObject({ code: 'ai_provider_failed' })
    expect(store.getState().messages[1]).toMatchObject({ state: 'error' })
    expect(streamChat).toHaveBeenCalledTimes(1)
  })

  it('stop() aborts the stream and keeps the partial reply', async () => {
    vi.mocked(streamChat).mockImplementation(({ signal, onEvent }: StreamChatParams) => {
      onEvent({ kind: 'text', text: 'partial' } as never)
      return new Promise((_, reject) => {
        signal.addEventListener('abort', () => {
          reject(new DOMException('aborted', 'AbortError'))
        })
      })
    })
    const store = new ChatStore('s')

    const pending = store.send('hi', vi.fn())
    store.stop()
    await pending

    expect(store.getState().streaming).toBe(false)
    expect(store.getState().error).toBeNull()
    expect(store.getState().messages[1]).toMatchObject({ text: 'partial', state: 'done' })
  })

  it('clear() empties the conversation', async () => {
    replyWith([{ kind: 'done' }])
    const store = new ChatStore('s')
    await store.send('hi', vi.fn())

    store.clear()

    expect(store.getState().messages).toEqual([])
  })

  it('keeps stores for different signed-in users isolated', () => {
    expect(getChatStore('s', 'user-1')).not.toBe(getChatStore('s', 'user-2'))
    expect(getChatStore('s', 'user-1')).toBe(getChatStore('s', 'user-1'))
  })

  it('stops and detaches user stores when the session is cleared', async () => {
    let emit!: StreamChatParams['onEvent']
    let reject!: (error: unknown) => void
    vi.mocked(streamChat).mockImplementation(
      ({ signal, onEvent }: StreamChatParams) =>
        new Promise((_, rejectPromise) => {
          emit = onEvent
          reject = rejectPromise
          signal.addEventListener('abort', () => undefined)
        }),
    )
    const store = getChatStore('s', 'user-1')
    const pending = store.send('private', vi.fn())

    clearSession()
    emit({ kind: 'text', text: 'late' })
    reject(new DOMException('aborted', 'AbortError'))
    await pending

    expect(store.getState()).toMatchObject({ streaming: false, error: null })
    expect(store.getState().messages[1]).toMatchObject({ text: '', state: 'done' })
    expect(getChatStore('s', 'user-1')).toBe(store)
  })
})

describe('ChatStore persistence', () => {
  const storageKey = chatStorageKey(null, 's')
  class MemoryPersistence implements ChatPersistence {
    readonly saved = new Map<string, ChatMessage[]>()
    loadResult: ChatMessage[] | null = null

    async load() {
      return this.loadResult
    }

    async save(slug: string, messages: ChatMessage[]) {
      this.saved.set(slug, messages)
    }

    async clear(slug: string) {
      this.saved.delete(slug)
    }
  }

  const storedTurn: ChatMessage[] = [
    { id: 'm1', role: 'user', text: 'earlier', activities: [], state: 'done' },
    { id: 'm2', role: 'assistant', text: 'answer', activities: [], state: 'done' },
  ]

  beforeEach(() => {
    resetChatStores()
    vi.mocked(streamChat).mockReset()
  })

  it('restores the stored conversation on creation', async () => {
    const persistence = new MemoryPersistence()
    persistence.loadResult = storedTurn
    const store = new ChatStore('s', persistence)

    await vi.waitFor(() => {
      expect(store.getState().messages).toEqual(storedTurn)
    })
  })

  it('ignores the stored conversation when the user already started a new one', async () => {
    let resolveLoad!: (messages: ChatMessage[] | null) => void
    const persistence: ChatPersistence = {
      load: () =>
        new Promise((resolve) => {
          resolveLoad = resolve
        }),
      save: async () => {},
      clear: async () => {},
    }
    replyWith([{ kind: 'done' }])
    const store = new ChatStore('s', persistence)

    await store.send('hi', vi.fn())
    resolveLoad(storedTurn)
    await vi.waitFor(() => {
      expect(store.getState().messages.map((message) => message.text)).toEqual(['hi', ''])
    })
  })

  it('does not restore a pending hydration after the user clears the chat', async () => {
    let resolveLoad!: (messages: ChatMessage[] | null) => void
    const persistence: ChatPersistence = {
      load: () =>
        new Promise((resolve) => {
          resolveLoad = resolve
        }),
      save: async () => {},
      clear: async () => {},
    }
    const store = new ChatStore('s', persistence)

    store.clear()
    resolveLoad(storedTurn)

    await vi.waitFor(() => {
      expect(store.getState().messages).toEqual([])
    })
  })

  it('does not let a cleared turn finish over a newer conversation', async () => {
    let rejectOld!: (error: unknown) => void
    let emitOld!: StreamChatParams['onEvent']
    vi.mocked(streamChat)
      .mockImplementationOnce(({ signal, onEvent }: StreamChatParams) =>
        new Promise((_, reject) => {
          rejectOld = reject
          emitOld = onEvent
          signal.addEventListener('abort', () => undefined)
        }),
      )
      .mockResolvedValueOnce(undefined)
    const store = new ChatStore('s', new MemoryPersistence())

    const oldTurn = store.send('old', vi.fn())
    store.clear()
    await store.send('new', vi.fn())
    emitOld({ kind: 'error', code: 'late_error', message: 'late' })
    rejectOld(new DOMException('aborted', 'AbortError'))
    await oldTurn

    expect(store.getState()).toMatchObject({ streaming: false, error: null })
    expect(store.getState().messages.map((message) => message.text)).toEqual(['new', ''])
  })

  it('saves the conversation after a turn, debounced', async () => {
    vi.useFakeTimers()
    try {
      replyWith([{ kind: 'text', text: 'hello' }, { kind: 'done' }])
      const persistence = new MemoryPersistence()
      const store = new ChatStore('s', persistence)

      await store.send('hi', vi.fn())
      expect(persistence.saved.has(storageKey)).toBe(false)

      await vi.advanceTimersByTimeAsync(500)
      expect(persistence.saved.get(storageKey)).toHaveLength(2)
    } finally {
      vi.useRealTimers()
    }
  })

  it('clear() deletes the stored record and cancels the pending save', async () => {
    vi.useFakeTimers()
    try {
      replyWith([{ kind: 'done' }])
      const persistence = new MemoryPersistence()
      persistence.saved.set(storageKey, storedTurn)
      const store = new ChatStore('s', persistence)

      await store.send('hi', vi.fn())
      store.clear()
      await vi.advanceTimersByTimeAsync(1000)

      expect(persistence.saved.has(storageKey)).toBe(false)
      expect(store.getState().messages).toEqual([])
    } finally {
      vi.useRealTimers()
    }
  })
})
