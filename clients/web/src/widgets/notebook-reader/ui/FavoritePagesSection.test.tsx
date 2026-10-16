import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listFavoritePages } from '@/entities/page'
import type { FavoritePageEntry } from '@/entities/page'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { FavoritePagesSection } from './FavoritePagesSection'

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  listFavoritePages: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const ENTRY: FavoritePageEntry = {
  pageId: 'page-1',
  title: 'V60',
  path: '/brewing/v60',
  notebookId: 'nb-1',
  notebookTitle: 'Guides',
  notebookSlug: 'guides',
}

function renderSection(activePath: string | null = null) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <FavoritePagesSection notebookId="nb-1" activePath={activePath} />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(listFavoritePages).mockReset()
  vi.mocked(listFavoritePages).mockResolvedValue([ENTRY])
  useSessionStore.setState({ status: 'authenticated', user: USER })
})

describe('FavoritePagesSection', () => {
  it('never loads favorites for anonymous readers', () => {
    useSessionStore.setState({ status: 'anonymous', user: null })

    const { container } = renderSection()

    expect(container).toBeEmptyDOMElement()
    expect(listFavoritePages).not.toHaveBeenCalled()
  })

  it('lists the notebook favorites as encoded links', async () => {
    renderSection()

    const link = await screen.findByRole('link', { name: 'V60' })
    expect(link).toHaveAttribute('href', '/notebooks/guides/brewing/v60')
    expect(listFavoritePages).toHaveBeenCalledWith(
      expect.objectContaining({ notebookId: 'nb-1' }),
    )
    expect(screen.getByText('Favorites')).toBeInTheDocument()
  })

  it('marks the open page as current', async () => {
    renderSection('brewing/v60')

    const link = await screen.findByRole('link', { name: 'V60' })
    expect(link).toHaveAttribute('aria-current', 'page')
  })

  it('renders nothing when there are no favorites', async () => {
    vi.mocked(listFavoritePages).mockResolvedValue([])

    const { container } = renderSection()

    await waitFor(() => {
      expect(listFavoritePages).toHaveBeenCalled()
    })
    // Let the resolved promise land before declaring the section absent.
    await new Promise((resolve) => setTimeout(resolve, 0))
    expect(container).toBeEmptyDOMElement()
  })
})
