import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { Message } from '@ag-ui/core'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { THREAD_STORAGE_KEY_PREFIX } from './aiThreadStorage'
import { useAiAssistantSession } from './useAiAssistantSession'

const mocks = vi.hoisted(() => ({
  abortRun: vi.fn(),
  addMessage: vi.fn(),
  runAgent: vi.fn(),
  setMessages: vi.fn(),
}))

const fakeMessages: Message[] = [
  { id: 'u1', role: 'user', content: 'hello' },
  { id: 'a1', role: 'assistant', content: 'hi there' },
]

let currentMessages: Message[] = []

vi.mock('@ag-ui/client', () => ({
  HttpAgent: vi.fn(function HttpAgentMock(this: unknown, { threadId }: { threadId: string }) {
    return {
      threadId,
      get messages() {
        return currentMessages
      },
      abortRun: mocks.abortRun,
      addMessage: mocks.addMessage,
      setMessages: (messages: Message[]) => {
        currentMessages = messages
      },
      runAgent: mocks.runAgent,
    }
  }),
}))

vi.mock('react-i18next', async () => {
  const actual = await vi.importActual<typeof import('react-i18next')>('react-i18next')
  return {
    ...actual,
    useTranslation: () => ({ t: (key: string) => key }),
  }
})

const notebook = {
  id: 'notebook-1',
  slug: 'architecture-notes',
  title: 'Architecture Notes',
  visibility: 'private',
  canEdit: true,
  itemCount: 3,
  folderCount: 1,
  pageCount: 2,
  favoriteCount: 0,
  isFavoritedByMe: false,
  lastActivityAtUtc: '2026-06-01T00:00:00Z',
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
  publishedAtUtc: null,
  description: null,
  ownerId: 'user-1',
  authorDisplayName: 'Yao',
  isPublished: false,
} satisfies Notebook

const activePage = {
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
  plainTextContent: 'Page body',
  isArchived: false,
  archivedAtUtc: null,
  archivedByUserId: null,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
} satisfies NotebookItem

function wrapper({ children }: { children: React.ReactNode }) {
  return <>{children}</>
}

describe('useAiAssistantSession persistence', () => {
  beforeEach(() => {
    localStorage.clear()
    currentMessages = []
    vi.clearAllMocks()
  })

  it('restores messages from localStorage on mount', async () => {
    const threadKey = `codecafe:${notebook.slug}:${activePage.path}`
    localStorage.setItem(
      `${THREAD_STORAGE_KEY_PREFIX}${threadKey}`,
      JSON.stringify({
        version: 1,
        savedAt: new Date().toISOString(),
        messages: fakeMessages,
      }),
    )

    const { result } = renderHook(
      () =>
        useAiAssistantSession({
          enabled: true,
          endpointPath: '/api/ai/assistant',
          notebook,
          activePage,
        }),
      { wrapper },
    )

    await waitFor(() => {
      expect(result.current.messages).toHaveLength(2)
    })

    expect(result.current.messages[0].content).toBe('hello')
    expect(result.current.messages[1].content).toBe('hi there')
  })

  it('removes the stored thread when clear is called', async () => {
    const threadKey = `codecafe:${notebook.slug}:${activePage.path}`
    const storageKey = `${THREAD_STORAGE_KEY_PREFIX}${threadKey}`
    localStorage.setItem(
      storageKey,
      JSON.stringify({
        version: 1,
        savedAt: new Date().toISOString(),
        messages: fakeMessages,
      }),
    )

    const { result } = renderHook(
      () =>
        useAiAssistantSession({
          enabled: true,
          endpointPath: '/api/ai/assistant',
          notebook,
          activePage,
        }),
      { wrapper },
    )

    await waitFor(() => {
      expect(result.current.messages).toHaveLength(2)
    })

    act(() => {
      result.current.clear()
    })

    expect(localStorage.getItem(storageKey)).toBeNull()
    await waitFor(() => {
      expect(result.current.messages).toHaveLength(0)
    })
  })

  it('clears the previous page session when switching to a page without a stored thread', async () => {
    const threadKey = `codecafe:${notebook.slug}:${activePage.path}`
    localStorage.setItem(
      `${THREAD_STORAGE_KEY_PREFIX}${threadKey}`,
      JSON.stringify({
        version: 1,
        savedAt: new Date().toISOString(),
        messages: fakeMessages,
      }),
    )

    const otherPage = {
      ...activePage,
      id: 'page-2',
      title: 'Other',
      slug: 'other',
      path: 'guides/other',
    } satisfies NotebookItem

    const { result, rerender } = renderHook(
      ({ page }: { page: NotebookItem }) =>
        useAiAssistantSession({
          enabled: true,
          endpointPath: '/api/ai/assistant',
          notebook,
          activePage: page,
        }),
      { wrapper, initialProps: { page: activePage } },
    )

    await waitFor(() => {
      expect(result.current.messages).toHaveLength(2)
    })

    rerender({ page: otherPage })

    await waitFor(() => {
      expect(result.current.messages).toHaveLength(0)
    })
    expect(result.current.error).toBeNull()
    expect(result.current.toolActivities).toHaveLength(0)
  })
})
