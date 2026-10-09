import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { NotebookGrid } from './NotebookGrid'
import type { NotebookSummary } from '@/entities/notebook'

vi.mock('@/entities/notebook', () => ({ NotebookCard: ({ notebook }: { notebook: NotebookSummary }) => <li>{notebook.title}</li> }))
const notebook = { id: 'n1', title: 'Already loaded' } as NotebookSummary

describe('NotebookGrid error recovery', () => {
  it('keeps existing results visible when a later page fails', () => {
    render(<NotebookGrid items={[notebook]} isPending={false} isError onRetry={vi.fn()} emptyText="Empty" />)
    expect(screen.getByText('Already loaded')).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Failed to load')
    expect(screen.getByRole('button', { name: 'Retry' })).toBeInTheDocument()
  })

  it('announces initial loading to assistive technology', () => {
    render(<NotebookGrid items={[]} isPending isError={false} emptyText="Empty" />)
    expect(screen.getByRole('status')).toHaveTextContent('Loading')
  })
})
