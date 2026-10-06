import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listTrash, purgeTrashedNotebook, restoreTrashedNotebook } from '@/entities/notebook'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { TrashPage } from './TrashPage'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  listTrash: vi.fn(),
  restoreTrashedNotebook: vi.fn(),
  purgeTrashedNotebook: vi.fn(),
  emptyTrash: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const ENTRY = {
  notebookId: '11111111-1111-1111-1111-111111111111',
  title: 'Old Drafts',
  pageCount: 4,
  deletedAtUtc: '2026-01-07T12:00:00.000Z',
}

function renderTrash() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <TrashPage />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(listTrash).mockReset()
  vi.mocked(restoreTrashedNotebook).mockReset()
  vi.mocked(purgeTrashedNotebook).mockReset()
  vi.mocked(listTrash).mockResolvedValue({
    items: [ENTRY],
    page: 1,
    pageSize: 50,
    totalCount: 1,
    hasNextPage: false,
  })
  useSessionStore.setState({ status: 'authenticated', user: USER })
})

describe('TrashPage', () => {
  it('lists trashed notebooks and restores one on a single click', async () => {
    const user = userEvent.setup()
    renderTrash()

    await user.click(await screen.findByRole('button', { name: 'Restore' }))

    expect(restoreTrashedNotebook).toHaveBeenCalledWith(ENTRY.notebookId, expect.anything())
  })

  it('asks twice before purging forever', async () => {
    const user = userEvent.setup()
    renderTrash()

    const purgeButton = await screen.findByRole('button', { name: 'Delete forever' })
    await user.click(purgeButton)
    expect(purgeTrashedNotebook).not.toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Sure? No undo.' }))
    expect(purgeTrashedNotebook).toHaveBeenCalledWith(ENTRY.notebookId, expect.anything())
  })
})
