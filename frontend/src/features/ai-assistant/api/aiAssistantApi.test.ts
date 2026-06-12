import { describe, expect, it, vi } from 'vitest'
import { apiFetch } from '@/shared/api/client'
import {
  applyAiEditProposal,
  createAiEditProposal,
  discardAiEditProposal,
  getAiStatus,
} from './aiAssistantApi'

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
      enabled: true,
      endpointPath: '/api/ai/assistant',
      editEndpointPath: '/api/ai/edits',
      draftEndpointPath: '/api/ai/drafts',
    })

    await getAiStatus()

    expect(apiFetchMock).toHaveBeenCalledWith('/custom/ai/status')
  })

  it('posts AI edit requests to the discovered edit endpoint', async () => {
    apiFetchMock.mockResolvedValueOnce({ proposalId: '1' })

    await createAiEditProposal('/api/ai/edits', {
      notebookSlug: 'architecture-notes',
      activePagePath: 'guides/overview',
      prompt: 'Rewrite this page',
      operation: 'auto',
      locale: 'en',
      apply: false,
    })

    expect(apiFetchMock).toHaveBeenCalledWith('/api/ai/edits', {
      body: JSON.stringify({
        notebookSlug: 'architecture-notes',
        activePagePath: 'guides/overview',
        prompt: 'Rewrite this page',
        operation: 'auto',
        locale: 'en',
        apply: false,
      }),
      method: 'POST',
    })
  })

  it('posts to the discovered apply path', async () => {
    apiFetchMock.mockResolvedValueOnce({ applied: true })

    await applyAiEditProposal('/api/ai/edits/proposals/1/apply')

    expect(apiFetchMock).toHaveBeenCalledWith('/api/ai/edits/proposals/1/apply', {
      method: 'POST',
    })
  })

  it('deletes the discovered discard path', async () => {
    apiFetchMock.mockResolvedValueOnce(undefined)

    await discardAiEditProposal('/api/ai/edits/proposals/1')

    expect(apiFetchMock).toHaveBeenCalledWith('/api/ai/edits/proposals/1', {
      method: 'DELETE',
    })
  })
})
