import { describe, expect, it } from 'vitest'
import { getCaretOffset, getSelectionOffsets, parseEditableDom, setCaretOffset, setSelectionOffsets } from './editableDom'

function editable(html: string): HTMLElement {
  const element = document.createElement('div')
  element.innerHTML = html
  document.body.appendChild(element)
  return element
}

describe('parseEditableDom', () => {
  it('reads plain text as a single unmarked span', () => {
    expect(parseEditableDom(editable('hello'))).toEqual([{ text: 'hello', marks: [] }])
  })

  it('maps inline elements back to marks', () => {
    const spans = parseEditableDom(editable('a<strong>b<em>c</em></strong>d'))

    expect(spans).toEqual([
      { text: 'a', marks: [] },
      { text: 'b', marks: [{ kind: 'bold' }] },
      { text: 'c', marks: [{ kind: 'bold' }, { kind: 'italic' }] },
      { text: 'd', marks: [] },
    ])
  })

  it('keeps link hrefs and colour data attributes', () => {
    const spans = parseEditableDom(
      editable('<a href="https://example.com">site</a><span data-color="danger">hot</span>'),
    )

    expect(spans).toEqual([
      { text: 'site', marks: [{ kind: 'link', href: 'https://example.com' }] },
      { text: 'hot', marks: [{ kind: 'color', name: 'danger' }] },
    ])
  })

  it('ignores line-break elements', () => {
    expect(parseEditableDom(editable('a<br>b'))).toEqual([{ text: 'ab', marks: [] }])
  })
})

describe('caret offsets', () => {
  it('round-trips an offset through marked-up content', () => {
    const element = editable('ab<strong>cd</strong>ef')

    setCaretOffset(element, 4)

    expect(getCaretOffset(element)).toBe(4)
  })

  it('clamps past-the-end offsets to the end of the text', () => {
    const element = editable('abc')

    setCaretOffset(element, 99)

    expect(getCaretOffset(element)).toBe(3)
  })

  it('round-trips a non-collapsed selection', () => {
    const element = editable('ab<strong>cd</strong>ef')

    setSelectionOffsets(element, 1, 5)

    expect(getSelectionOffsets(element)).toEqual({ start: 1, end: 5 })
  })
})
