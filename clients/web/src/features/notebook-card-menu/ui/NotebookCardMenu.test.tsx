import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { deleteNotebook } from '@/entities/notebook'
import { NotebookCardMenu } from './NotebookCardMenu'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  deleteNotebook: vi.fn(),
}))

function renderMenu() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <NotebookCardMenu notebookId="nb-1" title="Espresso Notes" onSettings={vi.fn()} />
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(deleteNotebook).mockReset()
  vi.mocked(deleteNotebook).mockResolvedValue(null)
})

describe('NotebookCardMenu', () => {
  it('opens on click and deletes without a confirm step — the trash is the undo', async () => {
    const user = userEvent.setup()
    renderMenu()

    await user.click(screen.getByRole('button', { name: 'Menu for "Espresso Notes"' }))
    await user.click(screen.getByRole('menuitem', { name: 'Delete' }))

    expect(deleteNotebook).toHaveBeenCalledWith('nb-1')
  })

  it('closes on Escape without acting', async () => {
    const user = userEvent.setup()
    renderMenu()

    await user.click(screen.getByRole('button', { name: 'Menu for "Espresso Notes"' }))
    await user.keyboard('{Escape}')

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(deleteNotebook).not.toHaveBeenCalled()
  })
})
