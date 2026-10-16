import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { changePageSlug } from '@/entities/page'
import type { PageDetails } from '@/entities/page'
import { ApiError } from '@/shared/api'
import { ChangePageUrlDialog } from './ChangePageUrlDialog'

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  changePageSlug: vi.fn(),
}))

const PAGE: PageDetails = {
  id: 'page-1',
  notebookId: 'notebook-1',
  title: 'Grinding',
  path: '/setup/grinding',
  isArchived: false,
  isFavorite: false,
  shares: [],
  blocks: [],
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  updatedAtUtc: '2026-01-07T12:00:00.000Z',
}

function renderDialog({
  page = PAGE,
  movesDescendants = 0,
}: { page?: PageDetails | null; movesDescendants?: number } = {}) {
  const onClose = vi.fn()
  const onDone = vi.fn()
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } })

  const { container } = render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <ChangePageUrlDialog
          page={page}
          movesDescendants={movesDescendants}
          onClose={onClose}
          onDone={onDone}
        />
      </MemoryRouter>
    </QueryClientProvider>,
  )

  return { container, onClose, onDone }
}

beforeEach(() => {
  vi.mocked(changePageSlug).mockReset()
  vi.mocked(changePageSlug).mockResolvedValue({ ...PAGE, path: '/setup/filter-notes' })
})

describe('ChangePageUrlDialog', () => {
  it('starts from the page’s own segment and previews the whole path', () => {
    renderDialog()

    // Only the page's own slug is editable: the ancestors own the prefix.
    expect(screen.getByLabelText('Link')).toHaveValue('grinding')
    expect(screen.getByText('The page will answer at /setup/grinding')).toBeInTheDocument()
  })

  it('drafts the URL from the page title', () => {
    renderDialog({
      page: { ...PAGE, title: 'Café Notes', path: '/setup/untitled-4821' },
    })

    // The title decides the draft; the page's own segment is only the fallback.
    expect(screen.getByLabelText('Link')).toHaveValue('café-notes')
    expect(screen.getByText('The page will answer at /setup/café-notes')).toBeInTheDocument()
  })

  it('falls back to the current segment when the title has nothing to slug', () => {
    renderDialog({ page: { ...PAGE, title: '!!!', path: '/setup/grinding' } })

    expect(screen.getByLabelText('Link')).toHaveValue('grinding')
  })

  it('refuses a slug the server would reject, without asking it', async () => {
    const user = userEvent.setup()
    renderDialog()

    const input = screen.getByLabelText('Link')
    await user.clear(input)
    await user.type(input, 'grinding notes')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Use letters, digits and single hyphens.',
    )
    expect(vi.mocked(changePageSlug)).not.toHaveBeenCalled()
  })

  it('sends the trimmed, lower-cased slug and reports the new path', async () => {
    const user = userEvent.setup()
    const { onDone } = renderDialog()

    const input = screen.getByLabelText('Link')
    await user.clear(input)
    await user.type(input, '  Filter-Notes  ')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(vi.mocked(changePageSlug)).toHaveBeenCalledWith('page-1', 'filter-notes')
    expect(onDone).toHaveBeenCalledWith('/setup/filter-notes')
  })

  it('reports a slug another page already uses', async () => {
    const user = userEvent.setup()
    vi.mocked(changePageSlug).mockRejectedValue(
      new ApiError({
        status: 409,
        code: 'slug_already_taken',
        kind: 'Conflict',
        message: 'taken',
      }),
    )
    const { onDone } = renderDialog()

    const input = screen.getByLabelText('Link')
    await user.clear(input)
    await user.type(input, 'brewing')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Another page in this notebook already uses that link.',
    )
    expect(onDone).not.toHaveBeenCalled()
  })

  it('reports any other failure generically', async () => {
    const user = userEvent.setup()
    vi.mocked(changePageSlug).mockRejectedValue(new Error('offline'))
    renderDialog()

    const input = screen.getByLabelText('Link')
    await user.clear(input)
    await user.type(input, 'brewing')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not change the link. Please try again.',
    )
  })

  it('closes without a round trip when the slug is unchanged', async () => {
    const user = userEvent.setup()
    const { onDone } = renderDialog()

    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(vi.mocked(changePageSlug)).not.toHaveBeenCalled()
    expect(onDone).toHaveBeenCalledWith('/setup/grinding')
  })

  it('warns how many subpages would move with the page', () => {
    renderDialog({ movesDescendants: 3 })

    expect(
      screen.getByText('All 3 pages under this one move with it.'),
    ).toBeInTheDocument()
  })

  it('keeps quiet when nothing sits under the page', () => {
    renderDialog()

    expect(screen.queryByText(/move with it/)).not.toBeInTheDocument()
  })

  it('renders nothing without a page', () => {
    const { container } = renderDialog({ page: null })

    expect(container).toBeEmptyDOMElement()
  })
})
