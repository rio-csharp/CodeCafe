import { beforeEach, describe, expect, it, vi } from 'vitest'
import { apiFetch } from '@/shared/api'
import {
  appendMarkdownToPage,
  discardMarkdownUpload,
  importMarkdownAsPage,
  replacePageContentFromMarkdown,
  uploadMarkdownText,
} from './markdownImportApi'

vi.mock('@/shared/api', () => ({
  apiFetch: vi.fn(),
}))

const apiFetchMock = vi.mocked(apiFetch)

describe('markdownImportApi', () => {
  beforeEach(() => {
    apiFetchMock.mockReset()
  })

  it('uploads generated Markdown as multipart text/markdown', async () => {
    apiFetchMock.mockResolvedValue({ uploadId: 'upload-1' })

    await uploadMarkdownText('# Draft', 'draft.md')

    expect(apiFetchMock).toHaveBeenCalledWith('/api/notes/uploads/markdown', {
      method: 'POST',
      body: expect.any(FormData),
    })

    const body = apiFetchMock.mock.calls[0][1]?.body as FormData
    expect(body.get('fileName')).toBe('draft.md')
    const file = body.get('file')
    expect(file).toBeInstanceOf(File)
    expect((file as File).type).toBe('text/markdown')
  })

  it('creates a page from a Markdown upload', async () => {
    apiFetchMock.mockResolvedValue({ path: 'ai-draft' })

    await importMarkdownAsPage('architecture notes', {
      title: 'AI Draft',
      parentPath: 'guides',
      uploadId: 'upload-1',
      includeContent: false,
    })

    expect(apiFetchMock).toHaveBeenCalledWith(
      '/api/notes/notebooks/architecture%20notes/pages/import-markdown',
      {
        method: 'POST',
        body: JSON.stringify({
          title: 'AI Draft',
          parentPath: 'guides',
          uploadId: 'upload-1',
          includeContent: false,
        }),
      },
    )
  })

  it('encodes nested page paths for append and replace operations', async () => {
    apiFetchMock.mockResolvedValue({ path: 'guides/api design' })

    await appendMarkdownToPage('architecture-notes', 'guides/api design', {
      uploadId: 'upload-1',
      includeContent: false,
    })
    await replacePageContentFromMarkdown('architecture-notes', 'guides/api design', {
      uploadId: 'upload-2',
      includeContent: true,
    })

    expect(apiFetchMock).toHaveBeenNthCalledWith(
      1,
      '/api/notes/notebooks/architecture-notes/pages/guides/api%20design/append-markdown',
      {
        method: 'POST',
        body: JSON.stringify({
          uploadId: 'upload-1',
          includeContent: false,
        }),
      },
    )
    expect(apiFetchMock).toHaveBeenNthCalledWith(
      2,
      '/api/notes/notebooks/architecture-notes/pages/guides/api%20design/import-markdown',
      {
        method: 'PUT',
        body: JSON.stringify({
          uploadId: 'upload-2',
          includeContent: true,
        }),
      },
    )
  })

  it('deletes orphaned uploads idempotently', async () => {
    apiFetchMock.mockResolvedValue({ uploadId: 'upload 1', result: 'discarded' })

    await discardMarkdownUpload('upload 1')

    expect(apiFetchMock).toHaveBeenCalledWith('/api/notes/uploads/upload%201', {
      method: 'DELETE',
    })
  })
})
