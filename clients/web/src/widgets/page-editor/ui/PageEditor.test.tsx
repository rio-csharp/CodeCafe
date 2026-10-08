import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { PageDetails } from '@/entities/page'
import { setSelectionOffsets } from '../lib/editableDom'
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

    expect(onSave).toHaveBeenCalledWith('Grinding finer', [])
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
    expect(onSave).toHaveBeenCalledWith('Grinding', [])
  })

  it('turns paragraph edits into an Update op on save', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn()
    renderEditor({ onSave })

    // jsdom does not type into contenteditable; edit the DOM, then fire input.
    const block = screen.getByRole('textbox', { name: 'editor.paragraph' })
    block.textContent = 'Start with fresh beans!'
    fireEvent.input(block)
    await user.click(screen.getByRole('button', { name: 'editor.save' }))

    expect(onSave).toHaveBeenCalledWith('Grinding', [
      {
        kind: 'Update',
        blockId: 'block-1',
        content: { spans: [{ text: 'Start with fresh beans!', marks: [] }] },
        baseVersion: 1,
      },
    ])
  })

  it('surfaces a save failure inline', () => {
    renderEditor({ error: 'editor.saveFailed' })

    expect(screen.getByRole('alert')).toHaveTextContent('editor.saveFailed')
  })

  it('splits a paragraph on Enter and reports the new block as an insert', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn()
    renderEditor({ onSave })

    const block = screen.getByRole('textbox', { name: 'editor.paragraph' })
    fireEvent.keyDown(block, { key: 'Enter' })

    expect(screen.getAllByRole('textbox', { name: 'editor.paragraph' })).toHaveLength(2)

    await user.click(screen.getByRole('button', { name: 'editor.save' }))
    const [, ops] = onSave.mock.calls[0] as [string, unknown[]]
    expect(ops).toHaveLength(2)
    expect(ops[0]).toMatchObject({ kind: 'Update', blockId: 'block-1' })
    expect(ops[1]).toMatchObject({ kind: 'Insert', after: 'block-1' })
  })

  it('appends a focused paragraph when the empty canvas below is clicked', async () => {
    const user = userEvent.setup()
    renderEditor()

    await user.click(screen.getByRole('button', { name: 'editor.appendBlock' }))

    const paragraphs = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
    expect(paragraphs).toHaveLength(2)
    expect(paragraphs[1]).toHaveFocus()

    // A second click focuses the still-empty trailing paragraph instead of
    // piling on another one.
    await user.click(screen.getByRole('button', { name: 'editor.appendBlock' }))
    expect(screen.getAllByRole('textbox', { name: 'editor.paragraph' })).toHaveLength(2)
  })

  it('moves focus to the next block on ArrowDown when there is one', () => {
    const onSave = vi.fn()
    renderEditor({ onSave })

    const block = screen.getByRole('textbox', { name: 'editor.paragraph' })
    fireEvent.keyDown(block, { key: 'Enter' })
    const paragraphs = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
    expect(paragraphs).toHaveLength(2)

    // Caret at the end of the first block, then ArrowDown: focus moves down
    // and no extra block appears.
    setSelectionOffsets(paragraphs[0], 'Start with fresh beans.'.length, 'Start with fresh beans.'.length)
    fireEvent.keyDown(paragraphs[0], { key: 'ArrowDown' })

    expect(screen.getAllByRole('textbox', { name: 'editor.paragraph' })).toHaveLength(2)
    expect(screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]).toHaveFocus()
  })

  it('appends a focused paragraph when ArrowDown has nowhere to go', () => {
    renderEditor()

    const block = screen.getByRole('textbox', { name: 'editor.paragraph' })
    setSelectionOffsets(block, 'Start with fresh beans.'.length, 'Start with fresh beans.'.length)
    fireEvent.keyDown(block, { key: 'ArrowDown' })

    const paragraphs = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
    expect(paragraphs).toHaveLength(2)
    expect(paragraphs[1]).toHaveFocus()

    // The new trailing paragraph is empty: ArrowDown again reuses it instead
    // of piling on a third block.
    fireEvent.keyDown(paragraphs[1], { key: 'ArrowDown' })
    expect(screen.getAllByRole('textbox', { name: 'editor.paragraph' })).toHaveLength(2)
  })

  it('keeps a divider visible between blocks while editing', () => {
    const withDivider: PageDetails = {
      ...PAGE,
      blocks: [
        PAGE.blocks[0]!,
        {
          id: 'block-divider',
          parentBlockId: null,
          type: 'divider',
          content: {},
          sortKey: 'b',
          version: 1,
          updatedAtUtc: '2026-01-07T12:00:00.000Z',
        },
      ],
    }
    const { container } = renderEditor({ page: withDivider })

    const rule = container.querySelector('hr')
    expect(rule).not.toBeNull()
    expect(rule?.parentElement).toHaveClass('py-2')
  })

  it('converts a paragraph to a to-do via the slash menu and saves delete + insert', async () => {
    const user = userEvent.setup()
    const onSave = vi.fn()
    renderEditor({ onSave })

    const block = screen.getByRole('textbox', { name: 'editor.paragraph' })
    block.textContent = '/todo'
    fireEvent.input(block)
    fireEvent.keyDown(block, { key: 'Enter' })

    // The paragraph became a to-do: checkbox plus the text engine.
    expect(screen.getByRole('checkbox', { name: 'editor.todoToggle' })).toBeInTheDocument()
    expect(screen.getByRole('textbox', { name: 'editor.todo' })).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'editor.save' }))
    const [, ops] = onSave.mock.calls[0] as [string, Record<string, unknown>[]]
    expect(ops).toHaveLength(2)
    expect(ops[0]).toMatchObject({ kind: 'Delete', blockId: 'block-1' })
    expect(ops[1]).toMatchObject({
      kind: 'Insert',
      type: 'todo',
      content: { checked: false, spans: [] },
    })
  })
})
