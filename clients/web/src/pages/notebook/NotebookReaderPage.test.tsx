import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { NotebookDetails, PageTreeNode } from '@/entities/notebook'
import { getNotebookDetails, getNotebookTree } from '@/entities/notebook'
import { createPage, getPageByPath, updatePage } from '@/entities/page'
import type { PageDetails } from '@/entities/page'
import { ApiError } from '@/shared/api'
import { NotebookReaderPage } from './NotebookReaderPage'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  getNotebookDetails: vi.fn(),
  getNotebookTree: vi.fn(),
}))

vi.mock('@/entities/page', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/page')>()),
  getPageByPath: vi.fn(),
  updatePage: vi.fn(),
  createPage: vi.fn(),
}))

const NOTEBOOK: NotebookDetails = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Espresso Notes',
  description: 'Short and strong.',
  slug: 'espresso-notes',
  visibility: 'Public',
  hasAccessCode: false,
  shares: [],
  tags: ['coffee'],
  pageCount: 2,
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  updatedAtUtc: '2026-01-07T12:00:00.000Z',
  isOwner: false,
  canWrite: false,
}

const ROOTS: PageTreeNode[] = [
  {
    id: 'page-1',
    title: 'Grinding',
    path: '/grinding',
    sortOrder: 0,
    isArchived: false,
    isFavorite: false,
    children: [],
  },
  {
    id: 'page-2',
    title: 'Brewing',
    path: '/brewing',
    sortOrder: 1,
    isArchived: false,
    isFavorite: false,
    children: [],
  },
]

const TREE = { notebookId: NOTEBOOK.id, roots: ROOTS }

function page(title: string, path: string): PageDetails {
  return {
    id: 'page-1',
    notebookId: NOTEBOOK.id,
    title,
    path,
    isArchived: false,
    isFavorite: false,
    blocks: [
      {
        id: 'block-1',
        parentBlockId: null,
        type: 'paragraph',
        content: { spans: [{ text: 'Start with fresh beans.', marks: [] }] },
        sortKey: 'a',
        version: 1,
        updatedAtUtc: '2026-01-07T12:00:00.000Z',
      },
    ],
    createdAtUtc: '2026-01-01T00:00:00.000Z',
    updatedAtUtc: '2026-01-07T12:00:00.000Z',
  }
}

function pageWithHeadings(): PageDetails {
  return {
    ...page('Grinding', '/grinding'),
    blocks: [
      {
        id: 'heading-1',
        parentBlockId: null,
        type: 'heading',
        content: { level: 2, spans: [{ text: 'Beans', marks: [] }] },
        sortKey: 'a',
        version: 1,
        updatedAtUtc: '2026-01-07T12:00:00.000Z',
      },
      {
        id: 'heading-2',
        parentBlockId: null,
        type: 'heading',
        content: { level: 3, spans: [{ text: 'Grind size', marks: [] }] },
        sortKey: 'b',
        version: 1,
        updatedAtUtc: '2026-01-07T12:00:00.000Z',
      },
    ],
  }
}

const notFound = () =>
  new ApiError({ status: 404, code: 'notebooks.not_found', kind: 'NotFound', message: 'nope' })

function renderReader(initialPath: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })

  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/notebooks/:slug/*" element={<NotebookReaderPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(getNotebookDetails).mockReset()
  vi.mocked(getNotebookTree).mockReset()
  vi.mocked(getPageByPath).mockReset()
  vi.mocked(updatePage).mockReset()
  vi.mocked(createPage).mockReset()
  vi.mocked(getNotebookDetails).mockResolvedValue(NOTEBOOK)
  vi.mocked(getNotebookTree).mockResolvedValue(TREE)
})

describe('NotebookReaderPage', () => {
  it('opens the first page when the notebook root is requested', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes')

    expect(await screen.findByRole('heading', { level: 1, name: 'Grinding' })).toBeInTheDocument()
    expect(vi.mocked(getPageByPath)).toHaveBeenCalledWith(
      expect.objectContaining({ slug: 'espresso-notes', path: '/grinding' }),
    )
    expect(document.title).toBe('Grinding · Espresso Notes · CodeCafe')
  })

  it('renders the page blocks returned by the API', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes/grinding')

    expect(await screen.findByText('Start with fresh beans.')).toBeInTheDocument()
  })

  it('offers a way back to the menu from every page', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes/grinding')

    expect(await screen.findByRole('link', { name: 'Back home' })).toHaveAttribute('href', '/')
  })

  it('starts narrow and lets the reader go full width', async () => {
    const user = userEvent.setup()
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    const { container } = renderReader('/notebooks/espresso-notes/grinding')
    await screen.findByRole('heading', { level: 1, name: 'Grinding' })

    const toggle = screen.getByRole('button', { name: 'Full width' })
    expect(toggle).toHaveAttribute('aria-pressed', 'false')
    expect(container.querySelector('main > div')).toHaveClass('max-w-3xl')

    await user.click(toggle)

    expect(toggle).toHaveAttribute('aria-pressed', 'true')
    expect(container.querySelector('main > div')).not.toHaveClass('max-w-3xl')
  })

  it('drops the site chrome so only the reading surface remains', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes/grinding')
    await screen.findByRole('heading', { level: 1, name: 'Grinding' })

    expect(screen.queryByText('Brewed with ❤️ and caffeine')).not.toBeInTheDocument()
    expect(screen.queryByText("Today's Menu")).not.toBeInTheDocument()
  })

  it('lists the page headings in the outline panel', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(pageWithHeadings())

    renderReader('/notebooks/espresso-notes/grinding')
    await screen.findByRole('heading', { level: 1, name: 'Grinding' })

    const outline = screen.getByRole('navigation', { name: 'Outline' })
    expect(within(outline).getByRole('link', { name: 'Beans' })).toHaveAttribute(
      'href',
      '#block-heading-1',
    )
    expect(within(outline).getByRole('link', { name: 'Grind size' })).toBeInTheDocument()
    // The heading itself carries the matching id, or the link goes nowhere.
    expect(document.getElementById('block-heading-1')?.tagName).toBe('H2')
  })

  it('opens the panels as mobile drawers and closes them again', async () => {
    const user = userEvent.setup()
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes/grinding')
    await screen.findByRole('heading', { level: 1, name: 'Grinding' })

    const contentsPill = screen.getByRole('button', { name: 'Contents' })
    expect(contentsPill).toHaveAttribute('aria-pressed', 'false')
    expect(screen.getByRole('navigation', { name: 'Contents' }).closest('aside')).toHaveClass(
      'hidden',
    )

    await user.click(contentsPill)

    expect(contentsPill).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('navigation', { name: 'Contents' }).closest('aside')).toHaveClass(
      'fixed',
    )

    await user.click(screen.getByRole('button', { name: 'Close' }))

    expect(contentsPill).toHaveAttribute('aria-pressed', 'false')
    expect(screen.getByRole('navigation', { name: 'Contents' }).closest('aside')).toHaveClass(
      'hidden',
    )
  })

  it('offers the tree as a disclosure so mobile readers can reach it', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes/grinding')

    expect(await screen.findByRole('button', { name: 'Contents' })).toBeInTheDocument()
    expect(await screen.findByRole('navigation', { name: 'Contents' })).toBeInTheDocument()
  })

  it('serves the non-committal state when the notebook itself is missing', async () => {
    vi.mocked(getNotebookDetails).mockRejectedValue(notFound())

    renderReader('/notebooks/absent')

    expect(
      await screen.findByText("This notebook doesn't exist, or it isn't public yet."),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Back home' })).toHaveAttribute('href', '/')
    expect(vi.mocked(getNotebookTree)).not.toHaveBeenCalled()
  })

  it('keeps the tree beside a missing page so there is a way out', async () => {
    vi.mocked(getPageByPath).mockRejectedValue(notFound())

    renderReader('/notebooks/espresso-notes/gone')

    expect(await screen.findByText('This page is missing')).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Brewing' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Espresso Notes' })).toHaveAttribute(
      'href',
      '/espresso-notes',
    )
  })

  it('serves the empty state when the notebook has no visible pages', async () => {
    vi.mocked(getNotebookTree).mockResolvedValue({ notebookId: NOTEBOOK.id, roots: [] })

    renderReader('/notebooks/espresso-notes')

    expect(
      await screen.findByText("Nothing's been written on this menu yet"),
    ).toBeInTheDocument()
    expect(vi.mocked(getPageByPath)).not.toHaveBeenCalled()
  })

  it('lets a writer rename the page from the chrome', async () => {
    const user = userEvent.setup()
    vi.mocked(getNotebookDetails).mockResolvedValue({ ...NOTEBOOK, isOwner: true, canWrite: true })
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))
    vi.mocked(updatePage).mockResolvedValue(page('Grinding finer', '/grinding'))

    renderReader('/notebooks/espresso-notes/grinding')
    await user.click(await screen.findByRole('button', { name: 'Edit' }))

    const title = screen.getByRole('textbox', { name: 'Page title' })
    await user.clear(title)
    await user.type(title, 'Grinding finer')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(vi.mocked(updatePage)).toHaveBeenCalledWith('page-1', { title: 'Grinding finer' })
    // Saved and back in reading mode, with the reader's own chrome restored.
    expect(await screen.findByRole('heading', { level: 1, name: 'Grinding' })).toBeInTheDocument()
  })

  it('shows the server-provided reason when saving fails', async () => {
    const user = userEvent.setup()
    vi.mocked(getNotebookDetails).mockResolvedValue({ ...NOTEBOOK, isOwner: true, canWrite: true })
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))
    vi.mocked(updatePage).mockRejectedValue(
      new ApiError({
        status: 400,
        code: 'invalid_block_payload',
        kind: 'Validation',
        message: "The block payload does not match its type's contract.",
      }),
    )

    renderReader('/notebooks/espresso-notes/grinding')
    await user.click(await screen.findByRole('button', { name: 'Edit' }))

    const title = screen.getByRole('textbox', { name: 'Page title' })
    await user.type(title, 'x')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "The block payload does not match its type's contract.",
    )
  })

  it('hides the edit pill from readers without write access', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes/grinding')
    await screen.findByRole('heading', { level: 1, name: 'Grinding' })

    expect(screen.queryByRole('button', { name: 'Edit' })).not.toBeInTheDocument()
  })

  it('creates a page from the tree header and lands in edit mode', async () => {
    const user = userEvent.setup()
    vi.mocked(getNotebookDetails).mockResolvedValue({ ...NOTEBOOK, isOwner: true, canWrite: true })
    vi.mocked(getPageByPath).mockResolvedValue(page('Untitled', '/untitled'))
    vi.mocked(createPage).mockResolvedValue(page('Untitled', '/untitled'))

    renderReader('/notebooks/espresso-notes/grinding')
    await user.click(await screen.findByRole('button', { name: 'New page' }))

    expect(vi.mocked(createPage)).toHaveBeenCalledWith({
      slug: 'espresso-notes',
      title: 'Untitled',
      parentPath: null,
    })
    // Straight into edit mode on the new page, title ready to be named.
    expect(await screen.findByRole('textbox', { name: 'Page title' })).toHaveValue('Untitled')
  })

  it('creates a subpage under a tree node', async () => {
    const user = userEvent.setup()
    vi.mocked(getNotebookDetails).mockResolvedValue({ ...NOTEBOOK, isOwner: true, canWrite: true })
    vi.mocked(getPageByPath).mockResolvedValue(page('Untitled', '/grinding/untitled'))
    vi.mocked(createPage).mockResolvedValue(page('Untitled', '/grinding/untitled'))

    renderReader('/notebooks/espresso-notes/grinding')
    await user.click(await screen.findByRole('button', { name: 'New subpage under Grinding' }))

    expect(vi.mocked(createPage)).toHaveBeenCalledWith({
      slug: 'espresso-notes',
      title: 'Untitled',
      parentPath: '/grinding',
    })
    expect(await screen.findByRole('textbox', { name: 'Page title' })).toHaveValue('Untitled')
  })

  it('hides page creation from readers without write access', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes/grinding')
    await screen.findByRole('heading', { level: 1, name: 'Grinding' })

    expect(screen.queryByRole('button', { name: 'New page' })).not.toBeInTheDocument()
  })

  it('skips archived pages when picking the first page', async () => {
    vi.mocked(getNotebookTree).mockResolvedValue({
      notebookId: NOTEBOOK.id,
      roots: [
        { ...ROOTS[0]!, isArchived: true },
        ROOTS[1]!,
      ],
    })
    vi.mocked(getPageByPath).mockResolvedValue(page('Brewing', '/brewing'))

    renderReader('/notebooks/espresso-notes')

    expect(await screen.findByRole('heading', { level: 1, name: 'Brewing' })).toBeInTheDocument()
    expect(vi.mocked(getPageByPath)).toHaveBeenCalledWith(
      expect.objectContaining({ path: '/brewing' }),
    )
  })
})
