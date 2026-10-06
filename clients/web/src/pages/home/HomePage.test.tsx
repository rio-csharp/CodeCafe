import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listMyNotebooks, listNotebooks } from '@/entities/notebook'
import type { NotebookSummary } from '@/entities/notebook'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { HomePage } from './HomePage'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  listNotebooks: vi.fn(),
  listMyNotebooks: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const MINE: NotebookSummary = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Espresso Notes',
  description: 'Short and strong.',
  slug: 'espresso-notes',
  visibility: 'Private',
  isFavorite: false,
  tags: ['coffee'],
  pageCount: 3,
  updatedAtUtc: '2026-01-07T12:00:00.000Z',
}

const EMPTY_PAGE = {
  items: [],
  page: 1,
  pageSize: 12,
  totalCount: 0,
  hasNextPage: false,
}

function renderHomePage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(listNotebooks).mockReset()
  vi.mocked(listMyNotebooks).mockReset()
  vi.mocked(listNotebooks).mockResolvedValue(EMPTY_PAGE)
  useSessionStore.setState({ status: 'anonymous', user: null })
})

describe('HomePage', () => {
  it('keeps the anonymous homepage free of a shelf', async () => {
    renderHomePage()

    expect(await screen.findByText('Every notebook, freshly brewed.')).toBeInTheDocument()
    expect(screen.getByText("Today's Menu")).toBeInTheDocument()
    expect(screen.queryByText('My notebooks')).not.toBeInTheDocument()
    expect(vi.mocked(listMyNotebooks)).not.toHaveBeenCalled()
  })

  it('shows my notebooks above the catalog when signed in', async () => {
    useSessionStore.setState({ status: 'authenticated', user: USER })
    vi.mocked(listMyNotebooks).mockResolvedValue({
      items: [MINE],
      page: 1,
      pageSize: 12,
      totalCount: 1,
      hasNextPage: false,
    })

    renderHomePage()

    expect(await screen.findByText('My notebooks')).toBeInTheDocument()
    expect(await screen.findByText('Espresso Notes')).toBeInTheDocument()
    expect(vi.mocked(listMyNotebooks)).toHaveBeenCalledWith({ page: 1, signal: expect.anything() })
  })

  it('serves themed copy when the shelf is empty', async () => {
    useSessionStore.setState({ status: 'authenticated', user: USER })
    vi.mocked(listMyNotebooks).mockResolvedValue(EMPTY_PAGE)

    renderHomePage()

    expect(await screen.findByText('My notebooks')).toBeInTheDocument()
    expect(await screen.findByText('Your shelf is empty')).toBeInTheDocument()
  })
})
