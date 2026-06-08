import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { act, renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import {
  appendMarkdownToPage,
  discardMarkdownUpload,
  importMarkdownAsPage,
  replacePageContentFromMarkdown,
  uploadMarkdownText,
} from '@/entities/notebook'
import { generateAiNoteDraft } from '../api/aiAssistantApi'
import type { AiNoteDraftResponse } from './types'
import { useApplyAiNoteDraft, useGenerateAiNoteDraft } from './useAiNoteDraft'

vi.mock('@/entities/notebook', () => ({
  appendMarkdownToPage: vi.fn(),
  discardMarkdownUpload: vi.fn(),
  importMarkdownAsPage: vi.fn(),
  notesKeys: {
    all: ['notes'],
    detail: (slug: string) => ['notes', 'detail', slug],
    itemsRoot: (notebookId: string) => ['notes', 'items', notebookId],
  },
  replacePageContentFromMarkdown: vi.fn(),
  uploadMarkdownText: vi.fn(),
}))

vi.mock('../api/aiAssistantApi', () => ({
  generateAiNoteDraft: vi.fn(),
}))

const generateAiNoteDraftMock = vi.mocked(generateAiNoteDraft)
const uploadMarkdownTextMock = vi.mocked(uploadMarkdownText)
const importMarkdownAsPageMock = vi.mocked(importMarkdownAsPage)
const appendMarkdownToPageMock = vi.mocked(appendMarkdownToPage)
const replacePageContentFromMarkdownMock = vi.mocked(replacePageContentFromMarkdown)
const discardMarkdownUploadMock = vi.mocked(discardMarkdownUpload)

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
  parentId: 'folder-1',
  type: 'page',
  title: 'Overview',
  slug: 'overview',
  path: 'guides/overview',
  sortOrder: 0,
  contentFormat: 'tiptap_json',
  contentJson: null,
  plainTextContent: 'Current page body',
  isArchived: false,
  archivedAtUtc: null,
  archivedByUserId: null,
  createdAtUtc: '2026-06-01T00:00:00Z',
  updatedAtUtc: null,
}

function createQueryWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      mutations: { retry: false },
      queries: { retry: false },
    },
  })

  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  }

  return { queryClient, wrapper: Wrapper }
}

describe('useGenerateAiNoteDraft', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('sends notebook, active page, intent, prompt, and locale to the draft endpoint', async () => {
    const response: AiNoteDraftResponse = {
      markdown: '# Outline',
      title: 'Outline',
      intent: 'outline',
      notebookSlug: 'architecture-notes',
      pagePath: 'guides/overview',
      generatedAtUtc: '2026-06-01T00:00:00Z',
    }
    generateAiNoteDraftMock.mockResolvedValue(response)
    const { wrapper } = createQueryWrapper()

    const { result } = renderHook(
      () => useGenerateAiNoteDraft({
        activePage,
        draftEndpointPath: '/api/ai/drafts',
        locale: 'en-US',
        notebook,
      }),
      { wrapper },
    )

    let generated: AiNoteDraftResponse | null = null
    await act(async () => {
      generated = await result.current.mutateAsync({
        intent: 'outline',
        prompt: 'Create an outline.',
      })
    })

    expect(generated).toBe(response)
    expect(generateAiNoteDraftMock).toHaveBeenCalledWith('/api/ai/drafts', {
      activePagePath: 'guides/overview',
      intent: 'outline',
      locale: 'en-US',
      notebookSlug: 'architecture-notes',
      prompt: 'Create an outline.',
    })
  })
})

describe('useApplyAiNoteDraft', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    uploadMarkdownTextMock.mockResolvedValue({
      bytesReceived: 12,
      expiresAtUtc: '2026-06-01T00:05:00Z',
      fileName: 'ai-draft.md',
      mediaType: 'text/markdown',
      uploadId: 'upload-1',
    })
    importMarkdownAsPageMock.mockResolvedValue({ path: 'guides/ai-draft' } as never)
    appendMarkdownToPageMock.mockResolvedValue({ path: 'guides/overview' } as never)
    replacePageContentFromMarkdownMock.mockResolvedValue({ path: 'guides/overview' } as never)
    discardMarkdownUploadMock.mockResolvedValue({ uploadId: 'upload-1', result: 'discarded' })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('creates a sibling page from generated Markdown', async () => {
    const { queryClient, wrapper } = createQueryWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')
    const { result } = renderHook(
      () => useApplyAiNoteDraft({ activePage, notebook }),
      { wrapper },
    )

    await act(async () => {
      await result.current.mutateAsync({
        markdown: '# AI Draft',
        mode: 'create',
        title: 'AI Draft',
      })
    })

    expect(uploadMarkdownTextMock).toHaveBeenCalledWith('# AI Draft', 'ai-draft.md')
    expect(importMarkdownAsPageMock).toHaveBeenCalledWith('architecture-notes', {
      includeContent: false,
      parentPath: 'guides',
      title: 'AI Draft',
      uploadId: 'upload-1',
    })
    expect(discardMarkdownUploadMock).not.toHaveBeenCalled()
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['notes', 'items', 'notebook-1'] })
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['notes', 'detail', 'architecture-notes'] })
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['notes'] })
  })

  it('appends generated Markdown to the active page', async () => {
    const { wrapper } = createQueryWrapper()
    const { result } = renderHook(
      () => useApplyAiNoteDraft({ activePage, notebook }),
      { wrapper },
    )

    await act(async () => {
      await result.current.mutateAsync({
        markdown: '## Extra',
        mode: 'append',
        title: 'Extra',
      })
    })

    expect(appendMarkdownToPageMock).toHaveBeenCalledWith('architecture-notes', 'guides/overview', {
      includeContent: false,
      uploadId: 'upload-1',
    })
  })

  it('replaces the active page content from generated Markdown', async () => {
    const { wrapper } = createQueryWrapper()
    const { result } = renderHook(
      () => useApplyAiNoteDraft({ activePage, notebook }),
      { wrapper },
    )

    await act(async () => {
      await result.current.mutateAsync({
        markdown: '# Replacement',
        mode: 'replace',
        title: 'Replacement',
      })
    })

    expect(replacePageContentFromMarkdownMock).toHaveBeenCalledWith(
      'architecture-notes',
      'guides/overview',
      {
        includeContent: false,
        uploadId: 'upload-1',
      },
    )
  })

  it('discards the upload when applying a draft fails after upload', async () => {
    const error = new Error('append failed')
    appendMarkdownToPageMock.mockRejectedValue(error)
    const { wrapper } = createQueryWrapper()
    const { result } = renderHook(
      () => useApplyAiNoteDraft({ activePage, notebook }),
      { wrapper },
    )

    await expect(
      act(async () => {
        await result.current.mutateAsync({
          markdown: '## Extra',
          mode: 'append',
          title: 'Extra',
        })
      }),
    ).rejects.toThrow(error)

    expect(discardMarkdownUploadMock).toHaveBeenCalledWith('upload-1')
  })
})
