import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { importNotebook } from '@/entities/notebook'
import type { NotebookDetails } from '@/entities/notebook'
import { MARKDOWN_FILE_MAX_BYTES } from '@/shared/lib'
import { ImportNotebookButton } from './ImportNotebookButton'

vi.mock('@/entities/notebook', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/notebook')>()),
  importNotebook: vi.fn(),
}))

const IMPORTED: NotebookDetails = {
  id: 'nb-9',
  title: 'Recipes',
  description: null,
  slug: 'recipes',
  visibility: 'Private',
  hasAccessCode: false,
  tags: [],
  shares: [],
  pageCount: 1,
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  updatedAtUtc: '2026-01-01T00:00:00.000Z',
  isOwner: true,
  canWrite: true,
}

function renderButton() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route path="/" element={<ImportNotebookButton />} />
          <Route path="/notebooks/:slug/*" element={<div>reader stub</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

async function pickFile(container: HTMLElement, file: File) {
  const input = container.querySelector('input[type="file"]')
  expect(input).not.toBeNull()
  fireEvent.change(input as HTMLInputElement, { target: { files: [file] } })
}

beforeEach(() => {
  vi.mocked(importNotebook).mockReset()
  vi.mocked(importNotebook).mockResolvedValue(IMPORTED)
})

describe('ImportNotebookButton', () => {
  it('imports the picked markdown file and navigates into the new notebook', async () => {
    const user = userEvent.setup()
    const { container } = renderButton()

    await user.click(screen.getByRole('button', { name: 'Import' }))
    await pickFile(container, new File(['# Recipes\n'], 'recipes.md', { type: 'text/markdown' }))

    await waitFor(() => {
      expect(importNotebook).toHaveBeenCalledWith({ fileName: 'recipes.md', markdown: '# Recipes\n' })
    })
    expect(await screen.findByText('reader stub')).toBeInTheDocument()
  })

  it('rejects files over 4 MB before any request is made', async () => {
    const user = userEvent.setup()
    const { container } = renderButton()
    const big = new File(['x'], 'huge.md')
    Object.defineProperty(big, 'size', { value: MARKDOWN_FILE_MAX_BYTES + 1 })

    await user.click(screen.getByRole('button', { name: 'Import' }))
    await pickFile(container, big)

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'That file is over 4 MB — pick a smaller one.',
    )
    expect(importNotebook).not.toHaveBeenCalled()
  })

  it('surfaces a themed error when the import fails', async () => {
    const user = userEvent.setup()
    vi.mocked(importNotebook).mockRejectedValue(new Error('nope'))
    const { container } = renderButton()

    await user.click(screen.getByRole('button', { name: 'Import' }))
    await pickFile(container, new File(['# Nope\n'], 'nope.md'))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Import failed. Please try again.',
    )
  })
})
