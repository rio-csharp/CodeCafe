import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listTrashedPages, purgeTrashedPage, restoreTrashedPage } from '@/entities/page'
import type { TrashedPageEntry } from '@/entities/page'
import { PageTrashDialog } from './PageTrashDialog'

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  listTrashedPages: vi.fn(),
  restoreTrashedPage: vi.fn(),
  purgeTrashedPage: vi.fn(),
}))

const ENTRIES: TrashedPageEntry[] = [
  {
    pageId: 'page-1',
    title: 'Old brew log',
    slug: 'old-brew-log',
    descendantCount: 2,
    deletedAtUtc: '2026-01-02T00:00:00.000Z',
  },
  {
    pageId: 'page-2',
    title: 'Stale notes',
    slug: 'stale-notes',
    descendantCount: 0,
    deletedAtUtc: '2026-01-03T00:00:00.000Z',
  },
]

function paged(items: TrashedPageEntry[]) {
  return { items, page: 1, pageSize: 50, totalCount: items.length, hasNextPage: false }
}

function renderDialog() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  const invalidateSpy = vi.spyOn(client, 'invalidateQueries')
  render(
    <QueryClientProvider client={client}>
      <PageTrashDialog slug="espresso-notes" onClose={vi.fn()} />
    </QueryClientProvider>,
  )
  return { invalidateSpy }
}

beforeEach(() => {
  vi.mocked(listTrashedPages).mockReset()
  vi.mocked(restoreTrashedPage).mockReset()
  vi.mocked(purgeTrashedPage).mockReset()
  vi.mocked(listTrashedPages).mockResolvedValue(paged(ENTRIES))
})

describe('PageTrashDialog', () => {
  it('lists the notebook trashed pages with their subpage counts', async () => {
    renderDialog()

    expect(await screen.findByText('Old brew log')).toBeInTheDocument()
    expect(screen.getByText('Stale notes')).toBeInTheDocument()
    expect(screen.getByText(/2 subpages inside/)).toBeInTheDocument()
    expect(listTrashedPages).toHaveBeenCalledWith(
      expect.objectContaining({ slug: 'espresso-notes' }),
    )
  })

  it('shows an empty state when nothing is trashed', async () => {
    vi.mocked(listTrashedPages).mockResolvedValue(paged([]))
    renderDialog()

    expect(await screen.findByText('No pages in the trash.')).toBeInTheDocument()
  })

  it('restores a page and invalidates the tree', async () => {
    const user = userEvent.setup()
    vi.mocked(restoreTrashedPage).mockResolvedValue(null)
    const { invalidateSpy } = renderDialog()

    const row = (await screen.findByText('Old brew log')).closest('li')
    expect(row).not.toBeNull()
    await user.click(within(row as HTMLElement).getByRole('button', { name: 'Restore' }))

    expect(restoreTrashedPage).toHaveBeenCalledWith('page-1', expect.anything())
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: ['notebooks', 'tree', 'espresso-notes'],
    })
  })

  it('purges only after the confirm click arms twice', async () => {
    const user = userEvent.setup()
    vi.mocked(purgeTrashedPage).mockResolvedValue(null)
    const { invalidateSpy } = renderDialog()

    const row = (await screen.findByText('Stale notes')).closest('li')
    expect(row).not.toBeNull()
    const purgeButton = within(row as HTMLElement).getByRole('button', {
      name: 'Delete forever',
    })

    await user.click(purgeButton)
    expect(purgeTrashedPage).not.toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Sure? No undo.' }))
    expect(purgeTrashedPage).toHaveBeenCalledWith('page-2', expect.anything())
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: ['pages', 'trash', 'espresso-notes'],
    })
  })
})
