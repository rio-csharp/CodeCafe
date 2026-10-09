import { describe, expect, it } from 'vitest'
import { sanitizeStoredMessages, trimToLimit } from './persistence'
import type { ChatMessage } from './store'

function message(
  role: 'user' | 'assistant',
  text: string,
  extra?: Partial<ChatMessage>,
): ChatMessage {
  return { id: crypto.randomUUID(), role, text, activities: [], state: 'done', ...extra }
}

describe('sanitizeStoredMessages', () => {
  it('returns nothing for data that is not a message list', () => {
    expect(sanitizeStoredMessages(undefined)).toEqual([])
    expect(sanitizeStoredMessages('chat')).toEqual([])
    expect(sanitizeStoredMessages([null, 42, { role: 'robot', text: 'hi' }, { role: 'user' }])).toEqual(
      [],
    )
  })

  it('settles an interrupted stream: text stays, streaming becomes done, activities complete', () => {
    const stored = [
      message('user', 'hi'),
      message('assistant', 'partial', {
        state: 'streaming',
        activities: [{ callId: 'c1', tool: 'get_page', done: false }],
      }),
    ]

    const restored = sanitizeStoredMessages(stored)

    expect(restored).toHaveLength(2)
    expect(restored[1]).toMatchObject({
      text: 'partial',
      state: 'done',
      activities: [{ callId: 'c1', tool: 'get_page', done: true }],
    })
  })

  it('keeps the error state so a failed reply still reads as failed', () => {
    const restored = sanitizeStoredMessages([message('assistant', 'boom', { state: 'error' })])

    expect(restored[0]).toMatchObject({ state: 'error' })
  })
})

describe('trimToLimit', () => {
  it('keeps short conversations untouched', () => {
    const messages = [message('user', 'hi'), message('assistant', 'hello')]

    expect(trimToLimit(messages)).toBe(messages)
  })

  it('keeps the newest messages, cutting on a user-message boundary', () => {
    const messages = Array.from({ length: 120 }, (_, index) =>
      message(index % 2 === 0 ? 'user' : 'assistant', `m${index}`),
    )

    const trimmed = trimToLimit(messages)

    expect(trimmed.length).toBeLessThanOrEqual(100)
    expect(trimmed[0]!.role).toBe('user')
    expect(trimmed.at(-1)!.text).toBe('m119')
  })
})
