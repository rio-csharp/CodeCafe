import { useState } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { SpanDto } from '@/entities/block'
import { TextBlockEditor } from './TextBlockEditor'
import { setSelectionOffsets } from '../lib/editableDom'
import type { SlashTarget } from '../lib/blockTypes'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

function renderEditor(spans: SpanDto[], overrides: Partial<Parameters<typeof TextBlockEditor>[0]> = {}) {
  return render(
    <TextBlockEditor
      spans={spans}
      ariaLabel="paragraph"
      onChange={vi.fn()}
      onSplit={vi.fn()}
      onMergeBackward={vi.fn()}
      onFocusPrevious={vi.fn()}
      onFocusNext={vi.fn()}
      onFocusHandled={vi.fn()}
      {...overrides}
    />,
  )
}

function Harness({
  initial,
  onTransform,
}: {
  initial: SpanDto[]
  onTransform?: (target: SlashTarget, spans: SpanDto[]) => void
}) {
  const [spans, setSpans] = useState(initial)
  return (
    <TextBlockEditor
      spans={spans}
      ariaLabel="paragraph"
      onChange={setSpans}
      onSplit={vi.fn()}
      onMergeBackward={vi.fn()}
      onFocusPrevious={vi.fn()}
      onFocusNext={vi.fn()}
      onFocusHandled={vi.fn()}
      onTransform={onTransform}
    />
  )
}

describe('TextBlockEditor', () => {
  it('never reconciles against browser-mutated DOM, so typing cannot duplicate text', () => {
    render(<Harness initial={[{ text: 'hello', marks: [] }]} />)

    const element = screen.getByRole('textbox')
    // What the browser does on a keystroke: mutate the DOM, then fire input.
    element.textContent = 'hello world'
    fireEvent.input(element)

    // React did not rewrite the subtree: the DOM still holds exactly what was typed.
    expect(element.textContent).toBe('hello world')
  })

  it('remounts content on external changes instead of reconciling a dirty DOM', () => {
    const { rerender } = renderEditor([{ text: 'hello', marks: [] }])

    const element = screen.getByRole('textbox')
    // Browser-shaped mutation React knows nothing about.
    element.appendChild(document.createElement('br'))

    // An external change (e.g. a merge) must replace, not reconcile — or jsdom
    // and real browsers alike throw removeChild NotFoundError.
    expect(() => {
      rerender(
        <TextBlockEditor
          spans={[{ text: 'hello world', marks: [] }]}
          ariaLabel="paragraph"
          onChange={vi.fn()}
          onSplit={vi.fn()}
          onMergeBackward={vi.fn()}
          onFocusPrevious={vi.fn()}
          onFocusNext={vi.fn()}
          onFocusHandled={vi.fn()}
        />,
      )
    }).not.toThrow()

    expect(screen.getByRole('textbox').textContent).toBe('hello world')
  })

  it('reports Enter as a split at the caret offset', () => {
    const onSplit = vi.fn()
    renderEditor([{ text: 'hello', marks: [] }], { onSplit })

    const element = screen.getByRole('textbox')
    fireEvent.keyDown(element, { key: 'Enter' })

    expect(onSplit).toHaveBeenCalledOnce()
  })

  it('Shift+Enter inserts a soft line break instead of splitting', () => {
    const onSplit = vi.fn()
    const onChange = vi.fn()
    renderEditor([{ text: 'abc', marks: [] }], { onSplit, onChange })

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 3, 3)
    fireEvent.keyDown(element, { key: 'Enter', shiftKey: true })

    expect(onSplit).not.toHaveBeenCalled()
    expect(onChange).toHaveBeenCalledWith([{ text: 'abc\n', marks: [] }])
  })

  it('Ctrl+Enter neither splits nor inserts — it is the save shortcut', () => {
    const onSplit = vi.fn()
    const onChange = vi.fn()
    renderEditor([{ text: 'abc', marks: [] }], { onSplit, onChange })

    fireEvent.keyDown(screen.getByRole('textbox'), { key: 'Enter', ctrlKey: true })

    expect(onSplit).not.toHaveBeenCalled()
    expect(onChange).not.toHaveBeenCalled()
  })

  it('Shift+Enter splits when the block type disallows soft breaks', () => {
    const onSplit = vi.fn()
    const onChange = vi.fn()
    renderEditor([{ text: 'abc', marks: [] }], { onSplit, onChange, allowSoftBreak: false })

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 3, 3)
    fireEvent.keyDown(element, { key: 'Enter', shiftKey: true })

    expect(onSplit).toHaveBeenCalledOnce()
    expect(onChange).not.toHaveBeenCalled()
  })

  it('renders a sentinel br after a trailing soft break so the caret line is visible', () => {
    renderEditor([{ text: 'abc\n', marks: [] }])

    expect(screen.getByRole('textbox').querySelector('br')).not.toBeNull()
  })

  it('paste without a block handler inserts text as-is, normalizing Windows line endings', () => {
    const onChange = vi.fn()
    renderEditor([{ text: 'ab', marks: [] }], { onChange })

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 2, 2)
    fireEvent.paste(element, { clipboardData: { getData: () => 'x\r\ny' } })

    expect(onChange).toHaveBeenCalledWith([{ text: 'abx\ny', marks: [] }])
  })

  it('routes a single markdown line pasted into an EMPTY block to the block handler', () => {
    const onChange = vi.fn()
    const onPasteBlocks = vi.fn()
    renderEditor([], { onChange, onPasteBlocks })

    const element = screen.getByRole('textbox')
    fireEvent.paste(element, { clipboardData: { getData: () => '## Title' } })

    expect(onChange).not.toHaveBeenCalled()
    expect(onPasteBlocks).toHaveBeenCalledWith(0, [
      { type: 'heading', content: { level: 2, spans: [{ text: 'Title', marks: [] }] } },
    ])
  })

  it('inserts a single markdown line verbatim into a non-empty block', () => {
    const onChange = vi.fn()
    const onPasteBlocks = vi.fn()
    renderEditor([{ text: 'ab', marks: [] }], { onChange, onPasteBlocks })

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 2, 2)
    fireEvent.paste(element, { clipboardData: { getData: () => '## Title' } })

    expect(onPasteBlocks).not.toHaveBeenCalled()
    expect(onChange).toHaveBeenCalledWith([{ text: 'ab## Title', marks: [] }])
  })

  it('converts a markdown marker into a block type when Space is typed after it', () => {
    const onTransform = vi.fn()
    renderEditor([{ text: '##', marks: [] }], { onTransform })

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 2, 2)
    fireEvent.keyDown(element, { key: ' ' })

    expect(onTransform).toHaveBeenCalledWith({ type: 'heading', level: 2 }, [])
  })

  it('opens a code block on the third backtick', () => {
    const onTransform = vi.fn()
    renderEditor([{ text: '``', marks: [] }], { onTransform })

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 2, 2)
    fireEvent.keyDown(element, { key: '`' })

    expect(onTransform).toHaveBeenCalledWith({ type: 'code' }, [])
  })

  it('routes multi-line pastes to the block-level handler', () => {
    const onChange = vi.fn()
    const onPasteBlocks = vi.fn()
    renderEditor([{ text: 'ab', marks: [] }], { onChange, onPasteBlocks })

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 2, 2)
    fireEvent.paste(element, { clipboardData: { getData: () => '# Title\nbody' } })

    expect(onChange).not.toHaveBeenCalled()
    expect(onPasteBlocks).toHaveBeenCalledWith(2, [
      { type: 'heading', content: { level: 1, spans: [{ text: 'Title', marks: [] }] } },
      { type: 'paragraph', content: { spans: [{ text: 'body', marks: [] }] } },
    ])
  })

  it('reports Backspace at the start as a backward merge', () => {
    const onMergeBackward = vi.fn()
    renderEditor([{ text: 'hello', marks: [] }], { onMergeBackward })

    const element = screen.getByRole('textbox')
    element.focus()
    fireEvent.keyDown(element, { key: 'Backspace' })

    expect(onMergeBackward).toHaveBeenCalledOnce()
  })

  it('shows the toolbar on selection and applies bold through it', () => {
    render(<Harness initial={[{ text: 'hello world', marks: [] }]} />)

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 0, 5)
    fireEvent.mouseUp(element)

    const bold = screen.getByRole('button', { name: 'editor.marks.bold' })
    expect(bold).toHaveAttribute('aria-pressed', 'false')
    // Hovering tells the user the shortcut exists.
    expect(bold).toHaveAttribute('title', 'editor.marks.bold (Ctrl+B)')
    fireEvent.click(bold)

    // Applying a mark remounts the editable div; re-query it.
    const remounted = screen.getByRole('textbox')
    expect(remounted.querySelector('strong')?.textContent).toBe('hello')
    // Toggling an already-bold selection now reports the button as active.
    expect(screen.getByRole('button', { name: 'editor.marks.bold' })).toHaveAttribute(
      'aria-pressed',
      'true',
    )
  })

  it('shows the toolbar when the drag ends outside the block', () => {
    render(<Harness initial={[{ text: 'hello world', marks: [] }]} />)

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 0, 5)
    // The mouse comes up outside the block: the div never sees the mouseup.
    fireEvent.mouseUp(document.body)

    expect(screen.getByRole('toolbar')).toBeInTheDocument()
  })

  it('shows the toolbar after a keyboard select-all', () => {
    render(<Harness initial={[{ text: 'hello world', marks: [] }]} />)

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 0, 11)
    fireEvent.keyUp(element, { key: 'a', ctrlKey: true })

    expect(screen.getByRole('toolbar')).toBeInTheDocument()
  })

  it('shows the toolbar when the drag spills past the block boundary', () => {
    render(<Harness initial={[{ text: 'hello world', marks: [] }]} />)

    const element = screen.getByRole('textbox')
    const textNode = element.firstChild?.firstChild?.firstChild
    expect(textNode?.nodeType).toBe(Node.TEXT_NODE)
    // The drag starts in the block but ends down in the page: the focus node
    // lands outside the block entirely, so the in-block portion is clamped.
    window
      .getSelection()
      ?.setBaseAndExtent(textNode as Node, 2, document.body, document.body.childNodes.length)
    fireEvent.mouseUp(document.body)

    expect(screen.getByRole('toolbar')).toBeInTheDocument()

    // The toolbar acts on the in-block portion only: from the anchor to the
    // block's end.
    fireEvent.click(screen.getByRole('button', { name: 'editor.marks.bold' }))
    const remounted = screen.getByRole('textbox')
    expect(remounted.querySelector('strong')?.textContent).toBe('llo world')
  })

  it('shows a single toolbar, owned by the anchor block, when a selection spans blocks', () => {
    render(
      <>
        <Harness initial={[{ text: 'first', marks: [] }]} />
        <Harness initial={[{ text: 'second', marks: [] }]} />
      </>,
    )

    const [first, second] = screen.getAllByRole('textbox')
    window
      .getSelection()
      ?.setBaseAndExtent(
        first.firstChild?.firstChild?.firstChild as Node,
        1,
        second.firstChild?.firstChild?.firstChild as Node,
        3,
      )
    fireEvent.mouseUp(document.body)

    expect(screen.getAllByRole('toolbar')).toHaveLength(1)
  })

  it('applies marks through Ctrl+B without opening the toolbar', () => {
    render(<Harness initial={[{ text: 'hello world', marks: [] }]} />)

    const element = screen.getByRole('textbox')
    element.focus()
    setSelectionOffsets(element, 6, 11)
    fireEvent.keyDown(element, { key: 'b', ctrlKey: true })

    expect(screen.getByRole('textbox').querySelector('strong')?.textContent).toBe('world')
  })

  it('adds a link through the toolbar input', async () => {
    render(<Harness initial={[{ text: 'my site', marks: [] }]} />)

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 3, 7)
    fireEvent.mouseUp(element)
    fireEvent.click(screen.getByRole('button', { name: 'editor.marks.link' }))

    const input = screen.getByRole('textbox', { name: 'editor.linkPlaceholder' })
    fireEvent.change(input, { target: { value: 'https://example.com' } })
    fireEvent.click(screen.getByRole('button', { name: 'editor.applyLink' }))

    const anchor = screen.getByRole('textbox').querySelector('a')
    expect(anchor).toHaveAttribute('href', 'https://example.com')
    expect(anchor?.textContent).toBe('site')
  })

  it('prefills an existing link for editing and can remove it', () => {
    render(
      <Harness
        initial={[{ text: 'site', marks: [{ kind: 'link', href: 'https://old.test' }] }]}
      />,
    )

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 0, 4)
    fireEvent.mouseUp(element)
    fireEvent.click(screen.getByRole('button', { name: 'editor.marks.link' }))

    // The existing href is prefilled and editable.
    const input = screen.getByRole('textbox', { name: 'editor.linkPlaceholder' })
    expect(input).toHaveValue('https://old.test')

    fireEvent.change(input, { target: { value: 'https://new.test' } })
    fireEvent.click(screen.getByRole('button', { name: 'editor.applyLink' }))
    expect(screen.getByRole('textbox').querySelector('a')).toHaveAttribute(
      'href',
      'https://new.test',
    )

    // Reopen and remove it entirely.
    const remounted = screen.getByRole('textbox')
    setSelectionOffsets(remounted, 0, 4)
    fireEvent.mouseUp(remounted)
    fireEvent.click(screen.getByRole('button', { name: 'editor.marks.link' }))
    fireEvent.click(screen.getByRole('button', { name: 'editor.removeLink' }))

    expect(screen.getByRole('textbox').querySelector('a')).toBeNull()
  })

  it('replaces browser-created nodes instead of duplicating them when a mark lands', () => {
    // Regression: typing into an empty block creates DOM nodes React never
    // rendered; applying a mark must swap them out, not append a second copy.
    render(<Harness initial={[]} />)

    const element = screen.getByRole('textbox')
    element.textContent = 'hello world'
    fireEvent.input(element)

    setSelectionOffsets(element, 0, 5)
    fireEvent.mouseUp(element)
    fireEvent.click(screen.getByRole('button', { name: 'editor.marks.bold' }))

    const remounted = screen.getByRole('textbox')
    expect(remounted.textContent).toBe('hello world')
    expect(remounted.querySelector('strong')?.textContent).toBe('hello')
  })

  it('retracts the toolbar when the selection leaves the block', () => {    render(<Harness initial={[{ text: 'hello world', marks: [] }]} />)

    const element = screen.getByRole('textbox')
    setSelectionOffsets(element, 0, 5)
    fireEvent.mouseUp(element)
    expect(screen.getByRole('toolbar')).toBeInTheDocument()

    // Clicking elsewhere collapses the selection; the browser then fires
    // selectionchange on the document.
    window.getSelection()?.removeAllRanges()
    fireEvent(document, new Event('selectionchange'))

    expect(screen.queryByRole('toolbar')).not.toBeInTheDocument()
  })

  it('opens the slash menu on "/" and transforms through keyboard selection', () => {
    const onTransform = vi.fn()
    render(<Harness initial={[]} onTransform={onTransform} />)

    const element = screen.getByRole('textbox')
    element.textContent = '/tod'
    fireEvent.input(element)

    // The menu filters down to the to-do entry.
    expect(screen.getByRole('listbox')).toBeInTheDocument()
    expect(screen.getAllByRole('option')).toHaveLength(1)

    fireEvent.keyDown(element, { key: 'Enter' })

    expect(onTransform).toHaveBeenCalledWith({ type: 'todo' }, [])
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
  })

  it('Escape closes the slash menu without transforming or cancelling the editor', () => {
    const onTransform = vi.fn()
    render(<Harness initial={[]} onTransform={onTransform} />)

    const element = screen.getByRole('textbox')
    element.textContent = '/hea'
    fireEvent.input(element)
    expect(screen.getAllByRole('option')).toHaveLength(3)

    // Arrow navigation moves the active option...
    fireEvent.keyDown(element, { key: 'ArrowDown' })
    expect(screen.getAllByRole('option')[1]).toHaveAttribute('aria-selected', 'true')

    // ...and Escape dismisses the menu without side effects.
    fireEvent.keyDown(element, { key: 'Escape' })
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument()
    expect(onTransform).not.toHaveBeenCalled()
  })
})
