import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
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

function renderTree(activePath: string | null = null) {
  return render(
    <MemoryRouter initialEntries={[`/notebooks/book${activePath ?? ''}`]}>
      <PageTree slug="book" roots={ROOTS} activePath={activePath} />
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

  it('compares paths without caring about leading slashes', () => {
    renderTree('guide')

    expect(screen.getByRole('link', { name: 'Guide' })).toHaveAttribute('aria-current', 'page')
  })
})
