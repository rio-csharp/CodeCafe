import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getNotebookDetails, shareNotebook } from '@/entities/notebook'
import type { NotebookDetails } from '@/entities/notebook'
import { ApiError } from '@/shared/api'
import { ShareNotebookDialog } from './ShareNotebookDialog'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  getNotebookDetails: vi.fn(),
  shareNotebook: vi.fn(),
  revokeNotebookShare: vi.fn(),
  setNotebookAccessCode: vi.fn(),
}))

const NOTEBOOK: NotebookDetails = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Espresso Notes',
  description: null,
  slug: 'espresso-notes',
  visibility: 'Private',
  hasAccessCode: false,
  tags: [],
  shares: [],
  pageCount: 3,
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  updatedAtUtc: '2026-01-07T12:00:00.000Z',
  isOwner: true,
  canWrite: true,
}

function renderDialog() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <ShareNotebookDialog slug="espresso-notes" onClose={vi.fn()} />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(getNotebookDetails).mockReset()
  vi.mocked(shareNotebook).mockReset()
  vi.mocked(getNotebookDetails).mockResolvedValue(NOTEBOOK)
})

describe('ShareNotebookDialog', () => {
  it('maps share_target_not_found to its own copy', async () => {
    const user = userEvent.setup()
    vi.mocked(shareNotebook).mockRejectedValue(
      new ApiError({
        status: 404,
        code: 'share_target_not_found',
        kind: 'NotFound',
        message: 'no such user',
      }),
    )
    renderDialog()

    await screen.findByText('Not shared with anyone yet.')
    await user.type(screen.getByLabelText('Their account email'), 'ghost@example.com')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(await screen.findByText('No user with that email.')).toBeInTheDocument()
  })
})
