import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { listBlockRevisions, restoreBlockRevision } from '@/entities/page'
import type { BlockRevision } from '@/entities/page'
import { BlockHistoryDialog } from './BlockHistoryDialog'

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  listBlockRevisions: vi.fn(),
  restoreBlockRevision: vi.fn(),
}))

const EDITED: BlockRevision = {
  blockId: 'block-1',
  blockVersion: 3,
  changeKind: 'Updated',
  content: { spans: [{ text: 'Fresh beans', marks: [] }] },
  source: 'Human',
  createdAtUtc: '2026-01-07T12:00:00.000Z',
}

const AI_ADDED: BlockRevision = {
  blockId: 'block-1',
  blockVersion: 1,
  changeKind: 'Added',
  content: { spans: [{ text: 'Beans', marks: [] }] },
  source: 'Ai',
  createdAtUtc: '2026-01-01T12:00:00.000Z',
}

function renderDialog(onRestored = vi.fn()) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <BlockHistoryDialog
        pageId="page-1"
        blockId="block-1"
        onClose={vi.fn()}
        onRestored={onRestored}
      />
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(listBlockRevisions).mockReset()
  vi.mocked(restoreBlockRevision).mockReset()
})

describe('BlockHistoryDialog', () => {
  it('lists revisions with kind labels and an AI badge', async () => {
    vi.mocked(listBlockRevisions).mockResolvedValue({
      items: [EDITED, AI_ADDED],
      nextCursor: null,
    })
    renderDialog()

    expect(await screen.findByText('Edited')).toBeInTheDocument()
    expect(screen.getByText('Added')).toBeInTheDocument()
    expect(screen.getByText('AI')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: 'Restore this version' })).toHaveLength(2)
  })

  it('shows the empty state when the block has no history', async () => {
    vi.mocked(listBlockRevisions).mockResolvedValue({ items: [], nextCursor: null })
    renderDialog()

    expect(await screen.findByText('No history for this block yet.')).toBeInTheDocument()
  })

  it('restores a version and hands over to the exit-edit flow', async () => {
    const user = userEvent.setup()
    const onRestored = vi.fn()
    vi.mocked(listBlockRevisions).mockResolvedValue({ items: [EDITED], nextCursor: null })
    vi.mocked(restoreBlockRevision).mockResolvedValue(undefined)
    renderDialog(onRestored)

    await user.click(await screen.findByRole('button', { name: 'Restore this version' }))

    await waitFor(() => {
      expect(restoreBlockRevision).toHaveBeenCalledWith('page-1', 'block-1', 3)
    })
    await waitFor(() => {
      expect(onRestored).toHaveBeenCalledTimes(1)
    })
  })

  it('shows an inline error when the restore fails', async () => {
    const user = userEvent.setup()
    vi.mocked(listBlockRevisions).mockResolvedValue({ items: [EDITED], nextCursor: null })
    vi.mocked(restoreBlockRevision).mockRejectedValue(new Error('boom'))
    renderDialog()

    await user.click(await screen.findByRole('button', { name: 'Restore this version' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not restore. Please try again.',
    )
  })
})
