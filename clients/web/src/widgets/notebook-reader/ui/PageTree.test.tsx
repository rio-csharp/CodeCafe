import { fireEvent, render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import type { PageTreeNode } from '@/entities/notebook'
import { PageTree } from './PageTree'

const ROOTS: PageTreeNode[] = [
  {
    id: '1',
    title: 'Guide',
    path: '/guide',
    sortOrder: 0,
    isArchived: false,
    isFavorite: false,
    children: [
      {
        id: '2',
        title: 'Setup',
        path: '/guide/setup',
        sortOrder: 0,
        isArchived: false,
        isFavorite: false,
        children: [
          {
            id: '3',
            title: '深烘 notes',
            path: '/guide/setup/深烘-notes',
            sortOrder: 0,
            isArchived: false,
            isFavorite: false,
            children: [],
          },
        ],
      },
      {
        id: '4',
        title: 'Retired pour',
        path: '/guide/retired',
        sortOrder: 1,
        isArchived: true,
        isFavorite: false,
        children: [],
      },
    ],
  },
  {
    id: '5',
    title: 'Hidden stash',
    path: '/hidden',
    sortOrder: 1,
    isArchived: true,
    isFavorite: false,
    children: [],
  },
]

function renderTree(activePath: string | null = null, filter = '') {
  return render(
    <MemoryRouter initialEntries={[`/notebooks/book${activePath ?? ''}`]}>
      <PageTree slug="book" roots={ROOTS} activePath={activePath} filter={filter} />
    </MemoryRouter>,
  )
}

describe('PageTree', () => {
  it('renders the visible top level and links each node to its path', () => {
    renderTree()

    expect(screen.getByRole('navigation', { name: 'Contents' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Guide' })).toHaveAttribute(
      'href',
      '/notebooks/book/guide',
    )
  })

  it('hides archived pages', () => {
    renderTree()

    expect(screen.queryByRole('link', { name: 'Hidden stash' })).not.toBeInTheDocument()
  })

  it('keeps children folded until their branch is opened', async () => {
    const user = userEvent.setup()
    renderTree()

    expect(screen.queryByRole('link', { name: 'Setup' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Toggle the subsections of Guide' }))

    expect(screen.getByRole('link', { name: 'Setup' })).toBeInTheDocument()
    // An archived child stays hidden even inside an open branch.
    expect(screen.queryByRole('link', { name: 'Retired pour' })).not.toBeInTheDocument()
  })

  it('nests deeper levels under their parent when opened', async () => {
    const user = userEvent.setup()
    renderTree()

    await user.click(screen.getByRole('button', { name: 'Toggle the subsections of Guide' }))
    expect(screen.queryByRole('link', { name: '深烘 notes' })).not.toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Toggle the subsections of Setup' }))

    expect(screen.getByRole('link', { name: '深烘 notes' })).toHaveAttribute(
      'href',
      '/notebooks/book/guide/setup/%E6%B7%B1%E7%83%98-notes',
    )
  })

  it('auto-expands every ancestor of the active page', () => {
    renderTree('/guide/setup/深烘-notes')

    expect(screen.getByRole('link', { name: 'Setup' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: '深烘 notes' })).toBeInTheDocument()
  })

  it('marks the active page as the current one', () => {
    renderTree('/guide/setup')

    expect(screen.getByRole('link', { name: 'Setup' })).toHaveAttribute('aria-current', 'page')
    expect(screen.getByRole('link', { name: 'Guide' })).not.toHaveAttribute('aria-current')
  })

  it('offers subpage creation on each row when the handler is present', async () => {
    const user = userEvent.setup()
    const onAddChild = vi.fn()
    render(
      <MemoryRouter>
        <PageTree slug="book" roots={ROOTS} onAddChild={onAddChild} />
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: 'New subpage under Guide' }))

    expect(onAddChild).toHaveBeenCalledWith('/guide')
  })

  it('hides the subpage buttons without a handler', () => {
    renderTree()

    expect(screen.queryByRole('button', { name: /New subpage/ })).not.toBeInTheDocument()
  })

  it('compares paths without caring about leading slashes', () => {
    renderTree('guide')

    expect(screen.getByRole('link', { name: 'Guide' })).toHaveAttribute('aria-current', 'page')
  })

  it('filters pages by title, keeping the ancestor trail for context', () => {
    renderTree(null, 'setup')

    const match = screen.getByRole('link', { name: /Setup/ })
    expect(match).toHaveAttribute('href', '/notebooks/book/guide/setup')
    expect(match).toHaveTextContent('Guide')
    expect(screen.queryByRole('link', { name: 'Guide' })).not.toBeInTheDocument()
  })

  it('matches titles case-insensitively and skips archived pages', () => {
    renderTree(null, 'HIDDEN')

    expect(screen.getByText('No pages match your search')).toBeInTheDocument()
  })

  it('shows a polite empty state when nothing matches', () => {
    renderTree(null, 'espresso')

    expect(screen.getByText('No pages match your search')).toBeInTheDocument()
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument()
  })

  it('shows archived pages dimmed and badged to writers', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter>
        <PageTree slug="book" roots={ROOTS} canWrite />
      </MemoryRouter>,
    )

    const stash = screen.getByRole('link', { name: /Hidden stash/ })
    expect(stash).toHaveClass('opacity-60')
    expect(within(stash).getByText('Archived')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Toggle the subsections of Guide' }))
    expect(screen.getByRole('link', { name: /Retired pour/ })).toHaveClass('opacity-60')
  })

  it('offers drag handles and node menus to writers only', () => {
    render(
      <MemoryRouter>
        <PageTree
          slug="book"
          roots={ROOTS}
          canWrite
          onMovePage={vi.fn()}
          onToggleArchive={vi.fn()}
          onDeletePage={vi.fn()}
        />
      </MemoryRouter>,
    )

    expect(screen.getByRole('button', { name: 'Drag to move Guide' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Actions for "Guide"' })).toBeInTheDocument()
  })

  it('keeps rows plain for readers', () => {
    renderTree()

    expect(screen.queryByRole('button', { name: /Drag to move/ })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Actions for/ })).not.toBeInTheDocument()
  })

  it('archives and unarchives from the node menu', async () => {
    const user = userEvent.setup()
    const onToggleArchive = vi.fn()
    render(
      <MemoryRouter>
        <PageTree
          slug="book"
          roots={ROOTS}
          canWrite
          onToggleArchive={onToggleArchive}
          onDeletePage={vi.fn()}
        />
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: 'Actions for "Guide"' }))
    await user.click(screen.getByRole('menuitem', { name: 'Archive' }))
    expect(onToggleArchive).toHaveBeenCalledWith(expect.objectContaining({ id: '1' }))

    // The archived root offers the reverse action.
    await user.click(screen.getByRole('button', { name: 'Actions for "Hidden stash"' }))
    await user.click(screen.getByRole('menuitem', { name: 'Unarchive' }))
    expect(onToggleArchive).toHaveBeenCalledWith(expect.objectContaining({ id: '5' }))
  })

  it('hands delete intents to the caller from the node menu', async () => {
    const user = userEvent.setup()
    const onDeletePage = vi.fn()
    render(
      <MemoryRouter>
        <PageTree
          slug="book"
          roots={ROOTS}
          canWrite
          onToggleArchive={vi.fn()}
          onDeletePage={onDeletePage}
        />
      </MemoryRouter>,
    )

    await user.click(screen.getByRole('button', { name: 'Actions for "Guide"' }))
    await user.click(screen.getByRole('menuitem', { name: 'Delete' }))

    expect(onDeletePage).toHaveBeenCalledWith(expect.objectContaining({ id: '1' }))
  })

  it('drops a dragged page onto another as its child', () => {
    const onMovePage = vi.fn()
    render(
      <MemoryRouter>
        <PageTree slug="book" roots={ROOTS} canWrite onMovePage={onMovePage} />
      </MemoryRouter>,
    )

    const dataTransfer = { setData: vi.fn(), effectAllowed: '', dropEffect: '' }
    fireEvent.dragStart(screen.getByRole('button', { name: 'Drag to move Hidden stash' }), {
      dataTransfer,
    })
    // jsdom reports zero rects, which dropPositionAt reads as the row middle.
    const guideRow = screen.getByRole('link', { name: 'Guide' }).parentElement
    expect(guideRow).not.toBeNull()
    fireEvent.dragOver(guideRow as Element, { dataTransfer })
    fireEvent.drop(guideRow as Element, { dataTransfer })

    expect(onMovePage).toHaveBeenCalledWith('5', {
      parentPath: '/guide',
      afterPageId: '4',
    })
  })

  it('refuses to drop a page into its own subtree', async () => {
    const user = userEvent.setup()
    const onMovePage = vi.fn()
    render(
      <MemoryRouter>
        <PageTree slug="book" roots={ROOTS} canWrite onMovePage={onMovePage} />
      </MemoryRouter>,
    )
    await user.click(screen.getByRole('button', { name: 'Toggle the subsections of Guide' }))

    const dataTransfer = { setData: vi.fn(), effectAllowed: '', dropEffect: '' }
    fireEvent.dragStart(screen.getByRole('button', { name: 'Drag to move Guide' }), {
      dataTransfer,
    })
    const setupRow = screen.getByRole('link', { name: 'Setup' }).parentElement
    fireEvent.dragOver(setupRow as Element, { dataTransfer })
    fireEvent.drop(setupRow as Element, { dataTransfer })

    expect(onMovePage).not.toHaveBeenCalled()
  })
})
