import { describe, expect, it, vi } from 'vitest'
import type { Message } from '@ag-ui/core'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import {
  createAiContext,
  createPromptId,
  getMessageText,
  getVisibleMessages,
} from './aiAssistantUtils'

const notebook: Notebook = {
  id: 'notebook-1',
  ownerId: 'user-1',
  title: 'Architecture Notes',
  slug: 'architecture-notes',
  description: null,
  visibility: 'private',
  isPublished: false,
  authorDisplayName: 'Yao',
  itemCount: 3,
  folderCount: 1,
  pageCount: 2,
  favoriteCount: 0,
  isFavoritedByMe: false,
  lastActivityAtUtc: '2026-06-01T00:00:00Z',
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
  publishedAtUtc: null,
  canEdit: true,
}

const activePage: NotebookItem = {
  id: 'page-1',
  notebookId: 'notebook-1',
  parentId: null,
  type: 'page',
  title: 'Overview',
  slug: 'overview',
  path: 'guides/overview',
  sortOrder: 0,
  contentFormat: 'tiptap_json',
  contentJson: null,
  plainTextContent: 'A'.repeat(1300),
  isArchived: false,
  archivedAtUtc: null,
  archivedByUserId: null,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
}

describe('createAiContext', () => {
  it('includes notebook metadata for the assistant', () => {
    const [context] = createAiContext({ notebook, activePage: null })

    expect(context.description).toBe('Current CodeCafe notebook')
    expect(JSON.parse(context.value)).toMatchObject({
      title: 'Architecture Notes',
      slug: 'architecture-notes',
      canEdit: true,
      itemCount: 3,
      pageCount: 2,
    })
  })

  it('adds a bounded active page preview when a page is selected', () => {
    const context = createAiContext({ notebook, activePage })

    expect(context).toHaveLength(2)
    expect(JSON.parse(context[1].value)).toMatchObject({
      title: 'Overview',
      path: 'guides/overview',
      type: 'page',
      contentFormat: 'tiptap_json',
      plainTextPreview: 'A'.repeat(1200),
    })
  })
})

describe('getMessageText', () => {
  it('returns plain string message content', () => {
    expect(getMessageText({ id: '1', role: 'assistant', content: 'Hello' } as Message)).toBe('Hello')
  })

  it('joins AG-UI text parts and ignores non-text parts', () => {
    const message = {
      id: '1',
      role: 'assistant',
      content: [
        { type: 'text', text: 'Hello ' },
        { type: 'tool-call', id: 'tool-1' },
        { type: 'text', text: 'there' },
      ],
    } as unknown as Message

    expect(getMessageText(message)).toBe('Hello there')
  })
})

describe('getVisibleMessages', () => {
  it('keeps only user and assistant messages with visible text', () => {
    const messages = [
      { id: '1', role: 'system', content: 'hidden' },
      { id: '2', role: 'assistant', content: '  ' },
      { id: '3', role: 'user', content: 'Find notes' },
      { id: '4', role: 'assistant', content: 'Found notes' },
    ] as Message[]

    expect(getVisibleMessages(messages).map((message) => message.id)).toEqual(['3', '4'])
  })
})

describe('createPromptId', () => {
  it('uses crypto.randomUUID when available', () => {
    const randomUUID = vi.fn(() => 'prompt-id')
    vi.stubGlobal('crypto', { randomUUID })

    expect(createPromptId()).toBe('prompt-id')
    expect(randomUUID).toHaveBeenCalledOnce()

    vi.unstubAllGlobals()
  })
})
