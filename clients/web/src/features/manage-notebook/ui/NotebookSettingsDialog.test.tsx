import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getNotebookDetails, updateNotebook } from '@/entities/notebook'
import type { NotebookDetails } from '@/entities/notebook'
import { NotebookSettingsDialog } from './NotebookSettingsDialog'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  getNotebookDetails: vi.fn(),
  updateNotebook: vi.fn(),
  setNotebookTags: vi.fn(),
}))

const NOTEBOOK: NotebookDetails = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Espresso Notes',
  description: null,
  slug: 'espresso-notes',
  visibility: 'Private',
  hasAccessCode: false,
  tags: ['coffee'],
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
        <NotebookSettingsDialog slug="espresso-notes" onClose={vi.fn()} />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(getNotebookDetails).mockReset()
  vi.mocked(updateNotebook).mockReset()
  vi.mocked(getNotebookDetails).mockResolvedValue(NOTEBOOK)
})

describe('NotebookSettingsDialog', () => {
  it('saves edited basics as a patch', async () => {
    const user = userEvent.setup()
    vi.mocked(updateNotebook).mockResolvedValue(NOTEBOOK)
    renderDialog()

    await screen.findByDisplayValue('Espresso Notes')
    const basics = screen.getByRole('form', { name: 'Basics' })
    const titleInput = within(basics).getByDisplayValue('Espresso Notes')
    await user.clear(titleInput)
    await user.type(titleInput, 'Filter Notes')
    await user.click(within(basics).getByRole('button', { name: 'Save' }))

    expect(updateNotebook).toHaveBeenCalledWith(
      'espresso-notes',
      expect.objectContaining({ title: 'Filter Notes', visibility: 'Private' }),
    )
  })

  it('adds a tag on Enter and saves the whole set', async () => {
    const user = userEvent.setup()
    const { setNotebookTags } = await import('@/entities/notebook')
    vi.mocked(setNotebookTags).mockResolvedValue(null)
    renderDialog()

    await screen.findByDisplayValue('Espresso Notes')
    await user.type(screen.getByLabelText('Type and press Enter'), 'brewing{Enter}')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    // One save button drives both endpoints; only the dirty half rides out.
    expect(setNotebookTags).toHaveBeenCalledWith('espresso-notes', ['coffee', 'brewing'])
    expect(updateNotebook).not.toHaveBeenCalled()
  })
})
