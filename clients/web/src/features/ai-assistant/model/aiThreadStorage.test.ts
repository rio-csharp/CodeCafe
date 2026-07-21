import { describe, expect, it, vi } from 'vitest'
import type { Message } from '@ag-ui/core'
import { clearThread, loadThread, saveThread, THREAD_STORAGE_KEY_PREFIX } from './aiThreadStorage'

const threadKey = 'codecafe:test:page'
const storageKey = `${THREAD_STORAGE_KEY_PREFIX}${threadKey}`

function buildMessage(content: string): Message {
  return { id: `msg-${content}`, role: 'user', content }
}

describe('aiThreadStorage', () => {
  it('returns null when nothing is stored', () => {
    localStorage.removeItem(storageKey)
    expect(loadThread(threadKey)).toBeNull()
  })

  it('saves and loads a thread', () => {
    const messages: Message[] = [buildMessage('hello')]
    saveThread(threadKey, messages)
    const loaded = loadThread(threadKey)
    expect(loaded).not.toBeNull()
    expect(loaded?.messages).toEqual(messages)
  })

  it('drops stale threads older than 7 days', () => {
    const stale = {
      version: 1,
      savedAt: new Date(Date.now() - 8 * 24 * 60 * 60 * 1000).toISOString(),
      messages: [buildMessage('old')],
    }
    localStorage.setItem(storageKey, JSON.stringify(stale))
    expect(loadThread(threadKey)).toBeNull()
    expect(localStorage.getItem(storageKey)).toBeNull()
  })

  it('drops data with an unsupported version', () => {
    localStorage.setItem(
      storageKey,
      JSON.stringify({ version: 99, savedAt: new Date().toISOString(), messages: [] }),
    )
    expect(loadThread(threadKey)).toBeNull()
  })

  it('drops corrupt JSON silently', () => {
    localStorage.setItem(storageKey, 'not-json')
    expect(loadThread(threadKey)).toBeNull()
  })

  it('clears a stored thread', () => {
    saveThread(threadKey, [buildMessage('hi')])
    clearThread(threadKey)
    expect(loadThread(threadKey)).toBeNull()
  })

  it('swallows storage write errors', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('quota')
    })
    expect(() => saveThread(threadKey, [buildMessage('x')])).not.toThrow()
    vi.restoreAllMocks()
  })
})
