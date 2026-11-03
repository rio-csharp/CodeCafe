import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import type { NotebookDetails } from '@/entities/notebook'
import { NotebookReaderLayout } from './NotebookReaderLayout'
import type { NotebookReaderLayoutProps } from './NotebookReaderLayout'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

const NOTEBOOK: NotebookDetails = {
  id: 'nb-1',
  title: 'Brew log',
  description: null,
  slug: 'brew-log',
  visibility: 'Public',
  hasAccessCode: false,
  tags: [],
  shares: [],
  pageCount: 3,
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: '2026-01-02T00:00:00Z',
  isOwner: false,
  canWrite: false,
}

const PREV = { path: '/grind', title: 'Grind' }
const NEXT = { path: '/water', title: 'Water' }

function renderLayout(props: Partial<NotebookReaderLayoutProps> = {}) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })

  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <NotebookReaderLayout notebook={NOTEBOOK} roots={[]} {...props}>
          <p>body</p>
        </NotebookReaderLayout>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('NotebookReaderLayout navigation', () => {
  it('renders the foot pager and the phone toolbar while reading', () => {
    renderLayout({ prevPage: PREV, nextPage: NEXT })

    expect(screen.getByRole('navigation', { name: 'reader.pageNav' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Grind/ })).toHaveAttribute(
      'href',
      '/notebooks/brew-log/grind',
    )
    expect(screen.getByRole('link', { name: /Water/ })).toHaveAttribute(
      'href',
      '/notebooks/brew-log/water',
    )
    expect(screen.getByRole('button', { name: 'reader.contents' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'reader.outline' })).toBeInTheDocument()
  })

  it('steps every navigation affordance aside while a draft is open', () => {
    renderLayout({ prevPage: PREV, nextPage: NEXT, showNavigation: false })

    expect(screen.queryByRole('navigation', { name: 'reader.pageNav' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /Grind/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: /Water/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'reader.contents' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'reader.outline' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'reader.previousPage' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'reader.nextPage' })).not.toBeInTheDocument()
  })

  it('omits the pager when the page has no neighbours', () => {
    renderLayout()

    expect(screen.queryByRole('navigation', { name: 'reader.pageNav' })).not.toBeInTheDocument()
  })
})
