import 'fake-indexeddb/auto'
import { describe, expect, it } from 'vitest'
import { createIndexedDbPersistence, sanitizeStoredMessages, trimToLimit } from './persistence'
import type { ChatMessage } from './store'

function message(
  role: 'user' | 'assistant',
  text: string,
  extra?: Partial<ChatMessage>,
): ChatMessage {
  return { id: crypto.randomUUID(), role, text, activities: [], state: 'done', ...extra }
}

describe('IndexedDB persistence', () => {
  // Each test uses a fresh slug: records are keyed by slug, so tests stay
  // independent without tearing down the (fake) database between them.
  const freshSlug = () => `test-${crypto.randomUUID()}`

  it('returns null for a conversation that was never saved', async () => {
    expect(await createIndexedDbPersistence().load(freshSlug())).toBeNull()
  })

  it('round-trips a conversation, settling an interrupted stream as done', async () => {
    const persistence = createIndexedDbPersistence()
    const slug = freshSlug()
    await persistence.save(slug, [
      message('user', 'hi'),
      message('assistant', 'partial', {
        state: 'streaming',
        activities: [{ callId: 'c1', tool: 'get_page', done: false }],
      }),
    ])

    const loaded = await createIndexedDbPersistence().load(slug)

    expect(loaded).toMatchObject([
      { role: 'user', text: 'hi' },
      {
        role: 'assistant',
        text: 'partial',
        state: 'done',
        activities: [{ callId: 'c1', tool: 'get_page', done: true }],
      },
    ])
  })

  it('caps the stored conversation at the newest 100 messages', async () => {
    const persistence = createIndexedDbPersistence()
    const slug = freshSlug()
    const messages = Array.from({ length: 120 }, (_, index) =>
      message(index % 2 === 0 ? 'user' : 'assistant', `m${index}`),
    )

    await persistence.save(slug, messages)
    const loaded = await createIndexedDbPersistence().load(slug)

    expect(loaded!.length).toBeLessThanOrEqual(100)
    expect(loaded![0]!.role).toBe('user')
    expect(loaded!.at(-1)!.text).toBe('m119')
  })

  it('saving an empty conversation and clear() both remove the record', async () => {
    const persistence = createIndexedDbPersistence()
    const emptied = freshSlug()
    await persistence.save(emptied, [message('user', 'hi')])
    await persistence.save(emptied, [])
    expect(await createIndexedDbPersistence().load(emptied)).toBeNull()

    const cleared = freshSlug()
    await persistence.save(cleared, [message('user', 'hi')])
    await persistence.clear(cleared)
    expect(await createIndexedDbPersistence().load(cleared)).toBeNull()
  })
})

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
