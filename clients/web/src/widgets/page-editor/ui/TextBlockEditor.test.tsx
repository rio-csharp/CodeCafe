import { useState } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { SpanDto } from '@/entities/block'
import { TextBlockEditor } from './TextBlockEditor'

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

    expect(element.textContent).toBe('hello world')
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
})
