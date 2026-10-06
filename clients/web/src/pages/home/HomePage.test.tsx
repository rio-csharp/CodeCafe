import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createNotebook, listMyNotebooks, listNotebooks } from '@/entities/notebook'
import type { NotebookSummary } from '@/entities/notebook'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { HomePage } from './HomePage'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  listNotebooks: vi.fn(),
  listMyNotebooks: vi.fn(),
  createNotebook: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const MINE: NotebookSummary = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Espresso Notes',
  description: 'Short and strong.',
  slug: 'espresso-notes',
  visibility: 'Unlisted',
  isFavorite: true,
  tags: ['coffee'],
  pageCount: 3,
  updatedAtUtc: '2026-01-07T12:00:00.000Z',
  ownerDisplayName: 'Ada',
}

const PUBLIC: NotebookSummary = {
  id: '22222222-2222-2222-2222-222222222222',
  title: 'Shared Knowledge',
  description: null,
  slug: 'shared-knowledge',
  visibility: 'Public',
  isFavorite: false,
  tags: [],
  pageCount: 12,
  updatedAtUtc: '2026-01-06T12:00:00.000Z',
  ownerDisplayName: 'Grace',
}

const EMPTY_PAGE = {
  items: [],
  page: 1,
  pageSize: 12,
  totalCount: 0,
  hasNextPage: false,
}

function pageOf(...items: NotebookSummary[]) {
  return { ...EMPTY_PAGE, items, totalCount: items.length }
}

function renderHomePage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/notebooks/*" element={<div>reader stub</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(listNotebooks).mockReset()
  vi.mocked(listMyNotebooks).mockReset()
  vi.mocked(createNotebook).mockReset()
  vi.mocked(listNotebooks).mockResolvedValue(EMPTY_PAGE)
  vi.mocked(listMyNotebooks).mockResolvedValue(EMPTY_PAGE)
  useSessionStore.setState({ status: 'anonymous', user: null })
})

describe('HomePage', () => {
  it('greets anonymous visitors with a masthead and the public shelf only', async () => {
    vi.mocked(listNotebooks).mockResolvedValue(pageOf(PUBLIC))
    renderHomePage()

    expect(await screen.findByText('Shared Knowledge')).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 1, name: 'CodeCafe' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Create account' })).toBeInTheDocument()
    expect(screen.queryByText('My notebooks')).not.toBeInTheDocument()
  })

  it('shows signed-in readers their own shelf with ownership cues', async () => {
    useSessionStore.setState({ status: 'authenticated', user: USER })
    vi.mocked(listMyNotebooks).mockResolvedValue(pageOf(MINE))
    renderHomePage()

    expect(await screen.findByText('Espresso Notes')).toBeInTheDocument()
    expect(screen.getByText('My notebooks')).toBeInTheDocument()
    // Visibility badge and favorite star only appear on the "mine" shelf.
    const shelf = screen.getAllByRole('list')[0]
    expect(within(shelf).getByText('Unlisted')).toBeInTheDocument()
    expect(within(shelf).getByRole('button', { name: 'Unfavorite' })).toBeInTheDocument()
    // The masthead is for visitors.
    expect(screen.queryByRole('heading', { level: 1 })).not.toBeInTheDocument()
  })

  it('searches within the signed-in reader\'s own shelf', async () => {
    const user = userEvent.setup()
    useSessionStore.setState({ status: 'authenticated', user: USER })
    renderHomePage()

    await screen.findByText('My notebooks')
    const boxes = screen.getAllByRole('searchbox')
    await user.type(boxes[0], 'espresso')

    await vi.waitFor(() => {
      expect(vi.mocked(listMyNotebooks)).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'espresso' }),
      )
    })
  })

  it('creates a notebook from the dialog and lands on it', async () => {
    const user = userEvent.setup()
    useSessionStore.setState({ status: 'authenticated', user: USER })
    vi.mocked(createNotebook).mockResolvedValue({
      id: '33333333-3333-3333-3333-333333333333',
      title: 'Fresh Ideas',
      description: null,
      slug: 'fresh-ideas',
      visibility: 'Private',
      hasAccessCode: false,
      tags: [],
      pageCount: 0,
      createdAtUtc: '2026-01-08T00:00:00.000Z',
      updatedAtUtc: '2026-01-08T00:00:00.000Z',
      isOwner: true,
      canWrite: true,
    })
    renderHomePage()

    // Two copies exist (desktop corner + mobile row); jsdom renders both.
    await user.click((await screen.findAllByRole('button', { name: 'New notebook' }))[0])
    await user.type(screen.getByLabelText('Title'), 'Fresh Ideas')
    await user.click(screen.getByRole('button', { name: 'Create' }))

    expect(vi.mocked(createNotebook)).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Fresh Ideas', visibility: 'Private' }),
    )
    expect(await screen.findByText('reader stub')).toBeInTheDocument() // navigated to the reader
  })

  it('filters the public shelf by the debounced search', async () => {
    const user = userEvent.setup()
    renderHomePage()

    await user.type(await screen.findByRole('searchbox'), 'rust')

    await screen.findByDisplayValue('rust')
    await vi.waitFor(() => {
      expect(vi.mocked(listNotebooks)).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'rust' }),
      )
    })
  })
})
