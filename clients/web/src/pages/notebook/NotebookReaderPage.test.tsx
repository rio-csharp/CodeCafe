import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { NotebookDetails, PageTreeNode } from '@/entities/notebook'
import { getNotebookDetails, getNotebookTree } from '@/entities/notebook'
import { getPageByPath } from '@/entities/page'
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
}))

const NOTEBOOK: NotebookDetails = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Espresso Notes',
  description: 'Short and strong.',
  slug: 'espresso-notes',
  visibility: 'Public',
  hasAccessCode: false,
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
  vi.mocked(getNotebookDetails).mockResolvedValue(NOTEBOOK)
  vi.mocked(getNotebookTree).mockResolvedValue(TREE)
})

describe('NotebookReaderPage', () => {
  it('opens the first page when the notebook root is requested', async () => {
    vi.mocked(getPageByPath).mockResolvedValue(page('Grinding', '/grinding'))

    renderReader('/notebooks/espresso-notes')

    expect(await screen.findByRole('heading', { level: 2, name: 'Grinding' })).toBeInTheDocument()
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
    expect(screen.getByRole('heading', { level: 1, name: 'Espresso Notes' })).toBeInTheDocument()
  })

  it('serves the empty state when the notebook has no visible pages', async () => {
    vi.mocked(getNotebookTree).mockResolvedValue({ notebookId: NOTEBOOK.id, roots: [] })

    renderReader('/notebooks/espresso-notes')

    expect(
      await screen.findByText("Nothing's been written on this menu yet"),
    ).toBeInTheDocument()
    expect(vi.mocked(getPageByPath)).not.toHaveBeenCalled()
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

    expect(await screen.findByRole('heading', { level: 2, name: 'Brewing' })).toBeInTheDocument()
    expect(vi.mocked(getPageByPath)).toHaveBeenCalledWith(
      expect.objectContaining({ path: '/brewing' }),
    )
  })
})
