import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it } from 'vitest'
import type { NotebookSummary } from '../model/types'
import { NotebookCard } from './NotebookCard'

const NOTEBOOK: NotebookSummary = {
  id: '11111111-1111-1111-1111-111111111111',
  title: 'Espresso Notes',
  description: 'Short and strong.',
  slug: 'espresso-notes',
  visibility: 'Public',
  isFavorite: false,
  tags: ['coffee', 'brewing'],
  pageCount: 3,
  updatedAtUtc: '2026-01-07T12:00:00.000Z',
}

function renderCard(notebook: NotebookSummary = NOTEBOOK) {
  return render(
    <MemoryRouter>
      <NotebookCard notebook={notebook} />
    </MemoryRouter>,
  )
}

describe('NotebookCard', () => {
  it('renders the title, tags and page count', () => {
    renderCard()

    expect(screen.getByRole('heading', { level: 3, name: 'Espresso Notes' })).toBeInTheDocument()
    expect(screen.getByText('coffee')).toBeInTheDocument()
    expect(screen.getByText('brewing')).toBeInTheDocument()
    expect(screen.getByText('3 pages')).toBeInTheDocument()
  })

  it('links the whole card to the notebook reader', () => {
    renderCard()

    expect(screen.getByRole('link', { name: /Espresso Notes/ })).toHaveAttribute(
      'href',
      '/notebooks/espresso-notes',
    )
  })

  it('omits the description when it is null', () => {
    renderCard({ ...NOTEBOOK, description: null })

    expect(screen.queryByText('Short and strong.')).not.toBeInTheDocument()
  })

  it('renders the description when it is present', () => {
    renderCard()

    expect(screen.getByText('Short and strong.')).toBeInTheDocument()
  })
})
