import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { setPageFavorite } from '@/entities/page'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { FavoritePageButton } from './FavoritePageButton'

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  setPageFavorite: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

function renderButton(isFavorite = false) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return {
    client,
    ...render(
      <QueryClientProvider client={client}>
        <FavoritePageButton pageId="page-1" slug="espresso-notes" isFavorite={isFavorite} />
      </QueryClientProvider>,
    ),
  }
}

beforeEach(() => {
  vi.mocked(setPageFavorite).mockReset()
  vi.mocked(setPageFavorite).mockResolvedValue(null)
  useSessionStore.setState({ status: 'authenticated', user: USER })
})

describe('FavoritePageButton', () => {
  it('stays hidden for anonymous readers', () => {
    useSessionStore.setState({ status: 'anonymous', user: null })

    const { container } = renderButton()

    expect(container.querySelector('button')).toBeNull()
  })

  it('favorites the page and refetches details, tree and favorites', async () => {
    const user = userEvent.setup()
    const { client } = renderButton(false)
    const invalidate = vi.spyOn(client, 'invalidateQueries')

    await user.click(screen.getByRole('button', { name: 'Favorite' }))

    await waitFor(() => {
      expect(setPageFavorite).toHaveBeenCalledWith('page-1', true)
    })
    await waitFor(() => {
      // ['pages'] prefix-matches the favorites lists too.
      expect(invalidate).toHaveBeenCalledWith({ queryKey: ['pages'] })
      expect(invalidate).toHaveBeenCalledWith({ queryKey: ['notebooks', 'tree', 'espresso-notes'] })
    })
  })

  it('unfavorites a favorited page', async () => {
    const user = userEvent.setup()
    renderButton(true)

    const toggle = screen.getByRole('button', { name: 'Unfavorite' })
    expect(toggle).toHaveAttribute('aria-pressed', 'true')

    await user.click(toggle)

    await waitFor(() => {
      expect(setPageFavorite).toHaveBeenCalledWith('page-1', false)
    })
  })

  it('shows a self-dismissing error bubble when the toggle fails', async () => {
    const user = userEvent.setup()
    vi.mocked(setPageFavorite).mockRejectedValue(new Error('nope'))
    renderButton()

    await user.click(screen.getByRole('button', { name: 'Favorite' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not update favorite. Please try again.',
    )

    await user.click(screen.getByRole('alert'))
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })
})
