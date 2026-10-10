import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { revokePageShare, sharePage } from '@/entities/page'
import type { PageDetails } from '@/entities/page'
import { ApiError } from '@/shared/api'
import { SharePageDialog } from './SharePageDialog'

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  sharePage: vi.fn(),
  revokePageShare: vi.fn(),
}))

const PAGE: PageDetails = {
  id: 'page-1',
  notebookId: 'notebook-1',
  title: 'Grinding',
  path: '/grinding',
  isArchived: false,
  isFavorite: false,
  shares: [
    { userId: 'u-bob', userName: 'Bob', role: 'Viewer' },
    { userId: 'u-cid', userName: 'Cid', role: 'Editor' },
  ],
  blocks: [],
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  updatedAtUtc: '2026-01-07T12:00:00.000Z',
}

function renderDialog(page: PageDetails | null = PAGE) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <SharePageDialog page={page} onClose={vi.fn()} />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(sharePage).mockReset()
  vi.mocked(revokePageShare).mockReset()
  vi.mocked(sharePage).mockResolvedValue(null)
  vi.mocked(revokePageShare).mockResolvedValue(null)
})

describe('SharePageDialog', () => {
  it('lists existing shares with their role badges', () => {
    renderDialog()

    // The badge sits next to the name inside the row; the same words also
    // exist as select options, so assert within the list item.
    const bobRow = screen.getByText('Bob').closest('li')
    expect(bobRow?.textContent).toContain('Viewer')
    const cidRow = screen.getByText('Cid').closest('li')
    expect(cidRow?.textContent).toContain('Editor')
  })

  it('revokes a share from its row', async () => {
    const user = userEvent.setup()
    renderDialog()

    await user.click(screen.getByRole('button', { name: 'Remove Bob' }))

    expect(revokePageShare).toHaveBeenCalledWith('page-1', 'u-bob')
  })

  it('adds a share with the picked role', async () => {
    const user = userEvent.setup()
    renderDialog()

    await user.type(screen.getByLabelText('Their account email'), 'dee@example.com')
    await user.selectOptions(screen.getByLabelText('Role'), 'Editor')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(sharePage).toHaveBeenCalledWith('page-1', 'dee@example.com', 'Editor')
  })

  it('maps share_target_not_found to its own copy', async () => {
    const user = userEvent.setup()
    vi.mocked(sharePage).mockRejectedValue(
      new ApiError({
        status: 404,
        code: 'share_target_not_found',
        kind: 'NotFound',
        message: 'no such user',
      }),
    )
    renderDialog({ ...PAGE, shares: [] })

    await user.type(screen.getByLabelText('Their account email'), 'ghost@example.com')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(await screen.findByText('No user with that email.')).toBeInTheDocument()
  })

  it('maps cannot_share_with_owner to its own copy', async () => {
    const user = userEvent.setup()
    vi.mocked(sharePage).mockRejectedValue(
      new ApiError({
        status: 400,
        code: 'cannot_share_with_owner',
        kind: 'Validation',
        message: 'owner',
      }),
    )
    renderDialog()

    await user.type(screen.getByLabelText('Their account email'), 'owner@example.com')
    await user.click(screen.getByRole('button', { name: 'Add' }))

    expect(await screen.findByText('The owner already has full access.')).toBeInTheDocument()
  })

  it('renders nothing when closed', () => {
    const { container } = renderDialog(null)

    expect(container).toBeEmptyDOMElement()
  })
})
