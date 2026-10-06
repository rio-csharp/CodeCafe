import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listNotebooks } from '@/entities/notebook'
import type { NotebookSummary } from '@/entities/notebook'
import { ApiError } from '@/shared/api'
import { NotebookCatalog } from './NotebookCatalog'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  listNotebooks: vi.fn(),
}))

const NOTEBOOK: NotebookSummary = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Espresso Notes',
  description: 'Short and strong.',
  slug: 'espresso-notes',
  visibility: 'Public',
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

function renderCatalog() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <NotebookCatalog search="" />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('NotebookCatalog', () => {
  beforeEach(() => {
    vi.mocked(listNotebooks).mockReset()
  })

  it('renders the notebooks returned by the query', async () => {
    vi.mocked(listNotebooks).mockResolvedValue({
      items: [NOTEBOOK],
      page: 1,
      pageSize: 12,
      totalCount: 1,
      hasNextPage: false,
    })

    renderCatalog()

    expect(await screen.findByText('Espresso Notes')).toBeInTheDocument()
    expect(screen.getByText('3 pages')).toBeInTheDocument()
  })

  it('shows the themed empty state when there is nothing to serve', async () => {
    vi.mocked(listNotebooks).mockResolvedValue(EMPTY_PAGE)

    renderCatalog()

    expect(await screen.findByText("The barista hasn't started yet")).toBeInTheDocument()
  })

  it('shows the themed error state with a retry button', async () => {
    vi.mocked(listNotebooks).mockRejectedValue(
      new ApiError({
        status: 500,
        code: 'server.unexpected',
        kind: 'Unexpected',
        message: 'boom',
      }),
    )

    renderCatalog()

    expect(
      await screen.findByRole('button', { name: 'Try again' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Something went wrong in the kitchen')).toBeInTheDocument()
  })

  it('refetches with the chosen sort', async () => {
    const user = userEvent.setup()
    vi.mocked(listNotebooks).mockResolvedValue(EMPTY_PAGE)

    renderCatalog()
    await screen.findByText("The barista hasn't started yet")
    expect(vi.mocked(listNotebooks).mock.calls[0]?.[0]).toMatchObject({
      sort: 'UpdatedDesc',
      page: 1,
    })

    await user.click(screen.getByRole('button', { name: 'By name' }))

    await waitFor(() => {
      expect(vi.mocked(listNotebooks)).toHaveBeenCalledTimes(2)
    })
    expect(vi.mocked(listNotebooks).mock.calls[1]?.[0]).toMatchObject({ sort: 'TitleAsc' })
  })

  it('loads the next serving while another page exists', async () => {
    const user = userEvent.setup()
    vi.mocked(listNotebooks).mockImplementation(async ({ page }) => ({
      items: [
        page === 1 ? NOTEBOOK : { ...NOTEBOOK, id: 'second-page-id', title: 'Cold Brew Logs' },
      ],
      page,
      pageSize: 12,
      totalCount: 24,
      hasNextPage: true,
    }))

    renderCatalog()
    await screen.findByText('Espresso Notes')

    await user.click(screen.getByRole('button', { name: 'One more serving' }))

    await waitFor(() => {
      expect(vi.mocked(listNotebooks)).toHaveBeenCalledTimes(2)
    })
    expect(vi.mocked(listNotebooks).mock.calls[1]?.[0]).toMatchObject({ page: 2 })
    expect(await screen.findByText('Cold Brew Logs')).toBeInTheDocument()
  })

  it('hides "one more serving" when the last page is reached', async () => {
    vi.mocked(listNotebooks).mockResolvedValue({
      items: [NOTEBOOK],
      page: 1,
      pageSize: 12,
      totalCount: 1,
      hasNextPage: false,
    })

    renderCatalog()
    await screen.findByText('Espresso Notes')

    expect(screen.queryByRole('button', { name: 'One more serving' })).not.toBeInTheDocument()
  })
})
