import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  changeNotebookSlug,
  getNotebookDetails,
  getNotebookSlugAvailability,
  updateNotebook,
} from '@/entities/notebook'
import type { NotebookDetails } from '@/entities/notebook'
import { ApiError } from '@/shared/api'
import { NotebookSettingsDialog } from './NotebookSettingsDialog'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  getNotebookDetails: vi.fn(),
  updateNotebook: vi.fn(),
  setNotebookTags: vi.fn(),
  changeNotebookSlug: vi.fn(),
  getNotebookSlugAvailability: vi.fn(),
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

/** The dialog plus a reader route, so a slug rename has somewhere to land. */
function renderDialogWithReader() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route
            path="/"
            element={<NotebookSettingsDialog slug="espresso-notes" onClose={vi.fn()} />}
          />
          <Route path="/notebooks/:slug/*" element={<p>the reader</p>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(getNotebookDetails).mockReset()
  vi.mocked(updateNotebook).mockReset()
  vi.mocked(changeNotebookSlug).mockReset()
  vi.mocked(getNotebookSlugAvailability).mockReset()
  vi.mocked(getNotebookDetails).mockResolvedValue(NOTEBOOK)
  vi.mocked(getNotebookSlugAvailability).mockResolvedValue({
    slug: 'filter-notes',
    isAvailable: true,
    suggestions: [],
  })
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

  it('never probes availability while the slug is unchanged', async () => {
    renderDialog()
    await screen.findByDisplayValue('espresso-notes')

    // Past the debounce window: an unchanged slug is the notebook's own.
    await new Promise((resolve) => setTimeout(resolve, 400))

    expect(getNotebookSlugAvailability).not.toHaveBeenCalled()
  })

  it('keeps save disabled while the new slug is malformed', async () => {
    const user = userEvent.setup()
    renderDialog()

    const slugInput = await screen.findByDisplayValue('espresso-notes')
    await user.clear(slugInput)
    await user.type(slugInput, '--bad')

    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled()
    expect(screen.getByRole('alert')).toHaveTextContent(
      'Lowercase letters, digits, CJK, and hyphens',
    )
    expect(changeNotebookSlug).not.toHaveBeenCalled()
  })

  it('renames the slug and follows the notebook to its new address', async () => {
    const user = userEvent.setup()
    vi.mocked(changeNotebookSlug).mockResolvedValue({ ...NOTEBOOK, slug: 'filter-notes' })
    renderDialogWithReader()

    const slugInput = await screen.findByDisplayValue('espresso-notes')
    await user.clear(slugInput)
    await user.type(slugInput, 'filter-notes')
    await screen.findByText('This link is available')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(changeNotebookSlug).toHaveBeenCalledWith('espresso-notes', 'filter-notes')
    expect(await screen.findByText('the reader')).toBeInTheDocument()
  })

  it('shows the themed copy when the server refuses the slug', async () => {
    const user = userEvent.setup()
    // The live probe said fine, but a race on the server says otherwise.
    vi.mocked(changeNotebookSlug).mockRejectedValue(
      new ApiError({
        status: 409,
        code: 'slug_already_taken',
        kind: 'Conflict',
        message: 'A notebook with this slug already exists.',
      }),
    )
    renderDialog()

    const slugInput = await screen.findByDisplayValue('espresso-notes')
    await user.clear(slugInput)
    await user.type(slugInput, 'filter-notes')
    await screen.findByText('This link is available')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('That link is taken.')
  })
})
