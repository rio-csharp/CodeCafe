import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { PageDetails } from '@/entities/page'
import { PageEditor } from './PageEditor'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

const PAGE: PageDetails = {
  id: 'page-1',
  notebookId: 'notebook-1',
  title: 'Grinding',
  path: '/grinding',
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

function renderEditor(overrides: Partial<Parameters<typeof PageEditor>[0]> = {}) {
  return render(
    <PageEditor page={PAGE} saving={false} onSave={vi.fn()} onCancel={vi.fn()} {...overrides} />,
  )
}

describe('PageEditor', () => {
  it('prefills the title and still shows the page content', () => {
    renderEditor()

    expect(screen.getByRole('textbox', { name: 'editor.titlePlaceholder' })).toHaveValue('Grinding')
    expect(screen.getByText('Start with fresh beans.')).toBeInTheDocument()
  })

  it('saves the trimmed title', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn()
    renderEditor({ onSave })

    const title = screen.getByRole('textbox', { name: 'editor.titlePlaceholder' })
    await user.clear(title)
    await user.type(title, '  Grinding finer  ')
    await user.click(screen.getByRole('button', { name: 'editor.save' }))

    expect(onSave).toHaveBeenCalledWith('Grinding finer')
  })

  it('refuses to save a blank title', async () => {
    const user = userEvent.setup()
    renderEditor()

    const title = screen.getByRole('textbox', { name: 'editor.titlePlaceholder' })
    await user.clear(title)

    expect(screen.getByRole('button', { name: 'editor.save' })).toBeDisabled()
  })

  it('cancels on Escape and saves on Ctrl+Enter', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn()
    const onCancel = vi.fn()
    renderEditor({ onSave, onCancel })

    const title = screen.getByRole('textbox', { name: 'editor.titlePlaceholder' })
    title.focus()
    await user.keyboard('{Escape}')
    expect(onCancel).toHaveBeenCalledOnce()

    await user.click(title)
    await user.keyboard('{Control>}{Enter}{/Control}')
    expect(onSave).toHaveBeenCalledWith('Grinding')
  })

  it('surfaces a save failure inline', () => {
    renderEditor({ error: 'editor.saveFailed' })

    expect(screen.getByRole('alert')).toHaveTextContent('editor.saveFailed')
  })
})
