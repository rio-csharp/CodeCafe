import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { searchPages } from '@/entities/page'
import type { PageSearchHit } from '@/entities/page'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { SearchPage } from './SearchPage'

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  searchPages: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const HIT_WITH_SNIPPET: PageSearchHit = {
  pageId: 'page-1',
  notebookId: 'nb-1',
  notebookSlug: 'guides',
  notebookTitle: 'Guides',
  title: 'V60',
  path: '/brewing/v60',
  snippet: '…pour in slow circles…',
}

const HIT_TITLE_ONLY: PageSearchHit = {
  pageId: 'page-2',
  notebookId: 'nb-1',
  notebookSlug: 'guides',
  notebookTitle: 'Guides',
  title: 'Rust 笔记',
  path: '/rust 笔记',
  snippet: '',
}

function renderSearch() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/search']}>
        <Routes>
          <Route path="/search" element={<SearchPage />} />
          <Route path="/login" element={<LoginProbe />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

/** Reveals where a redirect landed, including the `from` breadcrumb. */
function LoginProbe() {
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? ''
  return <p>{`signed-out:${from}`}</p>
}

beforeEach(() => {
  vi.mocked(searchPages).mockReset()
  vi.mocked(searchPages).mockResolvedValue({ items: [], nextCursor: null })
  useSessionStore.setState({ status: 'authenticated', user: USER })
})

describe('SearchPage', () => {
  it('sends anonymous visitors to login with a from breadcrumb', async () => {
    useSessionStore.setState({ status: 'anonymous', user: null })

    renderSearch()

    expect(await screen.findByText('signed-out:/search')).toBeInTheDocument()
    expect(searchPages).not.toHaveBeenCalled()
  })

  it('shows the idle prompt until something is typed', () => {
    renderSearch()

    expect(
      screen.getByText('Type above — every shelf you can reach gets rummaged.'),
    ).toBeInTheDocument()
    expect(searchPages).not.toHaveBeenCalled()
  })

  it('debounces typing before the first request leaves', async () => {
    const user = userEvent.setup()
    renderSearch()

    await user.type(screen.getByRole('searchbox'), 'rust')
    // Straight after the keystrokes the debounce window is still open.
    expect(searchPages).not.toHaveBeenCalled()

    await waitFor(() => {
      expect(searchPages).toHaveBeenCalledWith(
        expect.objectContaining({ query: 'rust', cursor: null }),
      )
    })
  })

  it('renders hits with notebook title, link, and snippet — hiding empty snippets', async () => {
    const user = userEvent.setup()
    vi.mocked(searchPages).mockResolvedValue({
      items: [HIT_WITH_SNIPPET, HIT_TITLE_ONLY],
      nextCursor: null,
    })
    renderSearch()

    await user.type(screen.getByRole('searchbox'), 'pour')

    const link = await screen.findByRole('link', { name: 'V60' })
    expect(link).toHaveAttribute('href', '/notebooks/guides/brewing/v60')
    // Both rows name their notebook; the title-only hit has no snippet paragraph.
    expect(screen.getAllByText('Guides')).toHaveLength(2)
    expect(screen.getByText('…pour in slow circles…')).toBeInTheDocument()

    const titleOnlyLink = screen.getByRole('link', { name: 'Rust 笔记' })
    expect(titleOnlyLink).toHaveAttribute(
      'href',
      `/notebooks/guides/${encodeURIComponent('rust 笔记')}`,
    )
    expect(titleOnlyLink.closest('li')?.textContent).not.toContain('…')
  })

  it('shows the themed empty state when nothing matches', async () => {
    const user = userEvent.setup()
    renderSearch()

    await user.type(screen.getByRole('searchbox'), 'decaf')

    expect(
      await screen.findByText('Nothing turned up — try a different blend of words.'),
    ).toBeInTheDocument()
  })
})
