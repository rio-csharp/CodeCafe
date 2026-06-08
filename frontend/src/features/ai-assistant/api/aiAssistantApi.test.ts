import { describe, expect, it, vi } from 'vitest'
import { apiFetch } from '@/shared/api/client'
import { generateAiNoteDraft, getAiStatus } from './aiAssistantApi'

vi.mock('@/shared/api/client', () => ({
  apiFetch: vi.fn(),
}))

vi.mock('@/shared/config', () => ({
  AI_STATUS_ENDPOINT_PATH: '/custom/ai/status',
}))

const apiFetchMock = vi.mocked(apiFetch)

describe('aiAssistantApi', () => {
  it('loads AI status from the configured discovery endpoint', async () => {
    apiFetchMock.mockResolvedValueOnce({
      draftEndpointPath: '/api/ai/drafts',
      enabled: true,
      endpointPath: '/api/ai/assistant',
    })

    await getAiStatus()

    expect(apiFetchMock).toHaveBeenCalledWith('/custom/ai/status')
  })

  it('posts AI draft requests to the discovered draft endpoint', async () => {
    apiFetchMock.mockResolvedValueOnce({
      generatedAtUtc: '2026-06-01T00:00:00Z',
      intent: 'custom',
      markdown: '# Draft',
      notebookSlug: 'architecture-notes',
      pagePath: null,
      title: 'Draft',
    })

    await generateAiNoteDraft('/api/ai/drafts', {
      activePagePath: null,
      intent: 'custom',
      locale: 'en',
      notebookSlug: 'architecture-notes',
      prompt: 'Draft a page.',
    })

    expect(apiFetchMock).toHaveBeenCalledWith('/api/ai/drafts', {
      body: JSON.stringify({
        activePagePath: null,
        intent: 'custom',
        locale: 'en',
        notebookSlug: 'architecture-notes',
        prompt: 'Draft a page.',
      }),
      method: 'POST',
    })
  })
})
