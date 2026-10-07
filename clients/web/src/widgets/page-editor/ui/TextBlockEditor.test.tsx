import { useState } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { SpanDto } from '@/entities/block'
import { TextBlockEditor } from './TextBlockEditor'
import { setSelectionOffsets } from '../lib/editableDom'

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

function Harness({ initial }: { initial: SpanDto[] }) {
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

  it('retracts the toolbar when the selection leaves the block', () => {
    render(<Harness initial={[{ text: 'hello world', marks: [] }]} />)

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
})
