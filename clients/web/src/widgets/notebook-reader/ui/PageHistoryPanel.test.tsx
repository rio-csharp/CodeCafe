import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { BlockDto } from '@/entities/block'
import { getPageAtRevision, listPageRevisions, restorePageRevision } from '@/entities/page'
import type { PageRevisionGroup, RevisionSource } from '@/entities/page'
import { PageHistoryPanel } from './PageHistoryPanel'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}))

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  listPageRevisions: vi.fn(),
  restorePageRevision: vi.fn(),
  getPageAtRevision: vi.fn(),
}))

const CURRENT: BlockDto[] = [
  {
    id: 'block-now',
    parentBlockId: null,
    type: 'paragraph',
    content: { spans: [{ text: 'current text', marks: [] }] },
    sortKey: 'a',
    version: 2,
    updatedAtUtc: '2026-01-07T12:00:00.000Z',
  },
]

const PAST: BlockDto[] = [
  {
    id: 'block-then',
    parentBlockId: null,
    type: 'paragraph',
    content: { spans: [{ text: 'old text', marks: [] }] },
    sortKey: 'a',
    version: 1,
    updatedAtUtc: '2026-01-06T12:00:00.000Z',
  },
]

function group(atUtc: string, source: RevisionSource = 'Human'): PageRevisionGroup {
  return {
    atUtc,
    source,
    changes: [
      { blockId: 'b1', blockVersion: 2, changeKind: 'Updated', content: {}, source, createdAtUtc: atUtc },
      { blockId: 'b2', blockVersion: 1, changeKind: 'Added', content: {}, source, createdAtUtc: atUtc },
    ],
  }
}

function renderPanel({ canWrite = true, onRestored = vi.fn() } = {}) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <PageHistoryPanel
        pageId="page-1"
        currentBlocks={CURRENT}
        canWrite={canWrite}
        onRestored={onRestored}
      />
    </QueryClientProvider>,
  )
}

describe('PageHistoryPanel', () => {
  beforeEach(() => {
    vi.mocked(listPageRevisions).mockReset()
    vi.mocked(restorePageRevision).mockReset()
    vi.mocked(getPageAtRevision).mockReset()
  })

  it('lists revision groups with a change summary and an AI badge', async () => {
    vi.mocked(listPageRevisions).mockResolvedValue({
      items: [group('2026-01-07T12:00:00.000Z'), group('2026-01-06T12:00:00.000Z', 'Ai')],
      nextCursor: null,
    })
    renderPanel()

    const summaries = await screen.findAllByText('history.updated · history.added')
    expect(summaries).toHaveLength(2)
    expect(screen.getByText('history.aiBadge')).toBeInTheDocument()
  })

  it('shows the empty state when there is no history', async () => {
    vi.mocked(listPageRevisions).mockResolvedValue({ items: [], nextCursor: null })
    renderPanel()

    expect(await screen.findByText('history.empty')).toBeInTheDocument()
  })

  it('hides restore from readers, even inside the preview dialog', async () => {
    const user = userEvent.setup()
    vi.mocked(listPageRevisions).mockResolvedValue({
      items: [group('2026-01-07T12:00:00.000Z')],
      nextCursor: null,
    })
    vi.mocked(getPageAtRevision).mockResolvedValue({
      atUtc: '2026-01-07T12:00:00.000Z',
      blocks: PAST,
    })
    renderPanel({ canWrite: false })

    await user.click(await screen.findByRole('button', { name: 'history.view' }))
    await screen.findByRole('dialog')

    expect(screen.queryByRole('button', { name: 'history.restore' })).not.toBeInTheDocument()
  })

  it('restores from the preview dialog with a single click and closes it', async () => {
    const user = userEvent.setup()
    const onRestored = vi.fn()
    vi.mocked(listPageRevisions).mockResolvedValue({
      items: [group('2026-01-07T12:00:00.000Z')],
      nextCursor: null,
    })
    vi.mocked(getPageAtRevision).mockResolvedValue({
      atUtc: '2026-01-07T12:00:00.000Z',
      blocks: PAST,
    })
    vi.mocked(restorePageRevision).mockResolvedValue(undefined)
    renderPanel({ onRestored })

    await user.click(await screen.findByRole('button', { name: 'history.view' }))
    const dialog = await screen.findByRole('dialog')
    await user.click(within(dialog).getByRole('button', { name: 'history.restore' }))

    expect(restorePageRevision).toHaveBeenCalledWith('page-1', '2026-01-07T12:00:00.000Z')
    await vi.waitFor(() => {
      expect(onRestored).toHaveBeenCalledOnce()
    })
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })

  it('opens a preview dialog with the snapshot and a diff against now', async () => {
    const user = userEvent.setup()
    vi.mocked(listPageRevisions).mockResolvedValue({
      items: [group('2026-01-07T12:00:00.000Z')],
      nextCursor: null,
    })
    vi.mocked(getPageAtRevision).mockResolvedValue({
      atUtc: '2026-01-07T12:00:00.000Z',
      blocks: PAST,
    })
    renderPanel()

    await user.click(await screen.findByRole('button', { name: 'history.view' }))

    const dialog = await screen.findByRole('dialog')
    expect(getPageAtRevision).toHaveBeenCalledWith(
      'page-1',
      '2026-01-07T12:00:00.000Z',
      expect.anything(),
    )
    // Current block is an addition, the past one a removal.
    expect(screen.getByText('history.diffAdded')).toBeInTheDocument()
    expect(screen.getByText('history.diffRemoved')).toBeInTheDocument()
    // The historical content renders inside the dialog.
    expect(dialog).toHaveTextContent('old text')
  })

  it('pages through history with the cursor', async () => {
    const user = userEvent.setup()
    vi.mocked(listPageRevisions)
      .mockResolvedValueOnce({ items: [group('2026-01-07T12:00:00.000Z')], nextCursor: 'cursor-2' })
      .mockResolvedValueOnce({ items: [group('2026-01-01T12:00:00.000Z')], nextCursor: null })
    renderPanel()

    await user.click(await screen.findByRole('button', { name: 'history.loadMore' }))

    expect(listPageRevisions).toHaveBeenLastCalledWith(
      expect.objectContaining({ pageId: 'page-1', cursor: 'cursor-2' }),
    )
    expect(await screen.findAllByText('history.updated · history.added')).toHaveLength(2)
  })
})
