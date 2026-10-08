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
    expect(ops).toContainEqual(expect.objectContaining({ kind: 'Delete', blockId: 'block-1' }))
    expect(ops).toContainEqual(
      expect.objectContaining({
        kind: 'Insert',
        type: 'todo',
        content: { checked: false, spans: [] },
      }),
    )
  })

  describe('nested blocks', () => {
    const NESTED: PageDetails = {
      ...PAGE,
      blocks: [
        PAGE.blocks[0]!,
        {
          id: 'child-1',
          parentBlockId: 'block-1',
          type: 'paragraph',
          content: { spans: [{ text: 'child', marks: [] }] },
          sortKey: 'a',
          version: 1,
          updatedAtUtc: '2026-01-07T12:00:00.000Z',
        },
      ],
    }

    it('renders children as editable blocks, indented under their parent', () => {
      renderEditor({ page: NESTED })

      const boxes = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
      expect(boxes).toHaveLength(2)
      expect(boxes[0]).toHaveTextContent('Start with fresh beans.')
      expect(boxes[1]).toHaveTextContent('child')
      // The child sits inside an indented container under its parent.
      expect(boxes[1]!.closest('div.ml-3')).not.toBeNull()
    })

    it('moves focus between parent and child with the arrow keys', () => {
      renderEditor({ page: NESTED })

      const boxes = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
      setSelectionOffsets(boxes[1]!, 0, 0)
      fireEvent.keyDown(boxes[1]!, { key: 'ArrowUp' })
      expect(boxes[0]).toHaveFocus()

      setSelectionOffsets(boxes[0]!, 'Start with fresh beans.'.length, 'Start with fresh beans.'.length)
      fireEvent.keyDown(boxes[0]!, { key: 'ArrowDown' })
      expect(boxes[1]).toHaveFocus()
    })

    it('splits a child block at the same depth', () => {
      const onSave = vi.fn()
      renderEditor({ page: NESTED, onSave })

      const child = screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]!
      setSelectionOffsets(child, 2, 2)
      fireEvent.keyDown(child, { key: 'Enter' })

      const boxes = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
      expect(boxes).toHaveLength(3)
      expect(boxes[2]).toHaveTextContent('ild')
      // The new block is nested too.
      expect(boxes[2]!.closest('div.ml-3')).not.toBeNull()

      fireEvent.click(screen.getByRole('button', { name: 'editor.save' }))
      const [, ops] = onSave.mock.calls[0] as [string, Record<string, unknown>[]]
      expect(ops).toContainEqual(
        expect.objectContaining({ kind: 'Insert', parent: 'block-1', after: 'child-1' }),
      )
    })

    it('merges a child into the previous visible block on Backspace', () => {
      const onSave = vi.fn()
      renderEditor({ page: NESTED, onSave })

      const child = screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]!
      setSelectionOffsets(child, 0, 0)
      fireEvent.keyDown(child, { key: 'Backspace' })

      const boxes = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
      expect(boxes).toHaveLength(1)
      expect(boxes[0]).toHaveTextContent('Start with fresh beans.child')

      fireEvent.click(screen.getByRole('button', { name: 'editor.save' }))
      const [, ops] = onSave.mock.calls[0] as [string, Record<string, unknown>[]]
      expect(ops).toContainEqual(
        expect.objectContaining({
          kind: 'Update',
          blockId: 'block-1',
          content: { spans: [{ text: 'Start with fresh beans.child', marks: [] }] },
        }),
      )
      expect(ops).toContainEqual(expect.objectContaining({ kind: 'Delete', blockId: 'child-1' }))
    })

    it('keeps a converted block at its depth', () => {
      const onSave = vi.fn()
      renderEditor({ page: NESTED, onSave })

      const child = screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]!
      child.textContent = '/quote'
      fireEvent.input(child)
      fireEvent.keyDown(child, { key: 'Enter' })

      const quote = screen.getByRole('textbox', { name: 'editor.quote' })
      expect(quote.closest('div.ml-3')).not.toBeNull()

      fireEvent.click(screen.getByRole('button', { name: 'editor.save' }))
      const [, ops] = onSave.mock.calls[0] as [string, Record<string, unknown>[]]
      expect(ops).toContainEqual(expect.objectContaining({ kind: 'Delete', blockId: 'child-1' }))
      expect(ops).toContainEqual(
        expect.objectContaining({ kind: 'Insert', type: 'quote', parent: 'block-1' }),
      )
    })

    it('outdents a child with Shift+Tab', () => {
      const onSave = vi.fn()
      renderEditor({ page: NESTED, onSave })

      fireEvent.keyDown(screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]!, {
        key: 'Tab',
        shiftKey: true,
      })

      // The block moved across levels, so it remounted: re-query it.
      const child = screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]!
      expect(child.closest('div.ml-3')).toBeNull()
      expect(child).toHaveTextContent('child')
      expect(child).toHaveFocus()

      fireEvent.click(screen.getByRole('button', { name: 'editor.save' }))
      const [, ops] = onSave.mock.calls[0] as [string, Record<string, unknown>[]]
      expect(ops).toEqual([
        expect.objectContaining({ kind: 'Move', blockId: 'child-1', after: 'block-1' }),
      ])
    })

    it('hoists children when their empty parent is deleted with Backspace', () => {
      const shell: PageDetails = {
        ...PAGE,
        blocks: [
          {
            id: 'shell',
            parentBlockId: null,
            type: 'paragraph',
            content: { spans: [] },
            sortKey: 'a',
            version: 1,
            updatedAtUtc: '2026-01-07T12:00:00.000Z',
          },
          {
            id: 'kid',
            parentBlockId: 'shell',
            type: 'paragraph',
            content: { spans: [{ text: 'kid', marks: [] }] },
            sortKey: 'a',
            version: 1,
            updatedAtUtc: '2026-01-07T12:00:00.000Z',
          },
        ],
      }
      renderEditor({ page: shell })

      const empty = screen.getAllByRole('textbox', { name: 'editor.paragraph' })[0]!
      setSelectionOffsets(empty, 0, 0)
      fireEvent.keyDown(empty, { key: 'Backspace' })

      const boxes = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
      expect(boxes).toHaveLength(1)
      expect(boxes[0]).toHaveTextContent('kid')
      expect(boxes[0]!.closest('div.ml-3')).toBeNull()
    })
  })

  describe('moving blocks', () => {
    const TWO: PageDetails = {
      ...PAGE,
      blocks: [
        PAGE.blocks[0]!,
        {
          id: 'block-2',
          parentBlockId: null,
          type: 'paragraph',
          content: { spans: [{ text: 'Second block', marks: [] }] },
          sortKey: 'b',
          version: 1,
          updatedAtUtc: '2026-01-07T12:00:00.000Z',
        },
      ],
    }

    it('indents a block with Tab and saves a Move op', () => {
      const onSave = vi.fn()
      renderEditor({ page: TWO, onSave })

      fireEvent.keyDown(screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]!, {
        key: 'Tab',
      })

      // The block moved across levels, so it remounted: re-query it.
      const second = screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]!
      expect(second.closest('div.ml-3')).not.toBeNull()
      expect(second).toHaveTextContent('Second block')
      expect(second).toHaveFocus()

      fireEvent.click(screen.getByRole('button', { name: 'editor.save' }))
      const [, ops] = onSave.mock.calls[0] as [string, Record<string, unknown>[]]
      expect(ops).toEqual([
        expect.objectContaining({ kind: 'Move', blockId: 'block-2', parent: 'block-1' }),
      ])
    })

    it('Tab on the first block does nothing', () => {
      const onSave = vi.fn()
      renderEditor({ page: TWO, onSave })

      const first = screen.getAllByRole('textbox', { name: 'editor.paragraph' })[0]!
      fireEvent.keyDown(first, { key: 'Tab' })

      fireEvent.click(screen.getByRole('button', { name: 'editor.save' }))
      const [, ops] = onSave.mock.calls[0] as [string, Record<string, unknown>[]]
      expect(ops).toEqual([])
    })

    it('reorders siblings with Ctrl+Shift+Arrow keys', () => {
      const onSave = vi.fn()
      renderEditor({ page: TWO, onSave })

      const second = screen.getAllByRole('textbox', { name: 'editor.paragraph' })[1]!
      fireEvent.keyDown(second, { key: 'ArrowUp', ctrlKey: true, shiftKey: true })

      const boxes = screen.getAllByRole('textbox', { name: 'editor.paragraph' })
      expect(boxes[0]).toHaveTextContent('Second block')

      fireEvent.click(screen.getByRole('button', { name: 'editor.save' }))
      const [, ops] = onSave.mock.calls[0] as [string, Record<string, unknown>[]]
      expect(ops).toEqual([expect.objectContaining({ kind: 'Move', blockId: 'block-2' })])
    })
  })
})
