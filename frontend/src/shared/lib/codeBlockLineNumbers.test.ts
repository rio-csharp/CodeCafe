import { describe, it, expect } from 'vitest'
import { applyCodeBlockLineNumbers } from './codeBlockLineNumbers'

function createContainerWithCode(text: string): [HTMLDivElement, HTMLPreElement] {
  const container = document.createElement('div')
  const pre = document.createElement('pre')
  const code = document.createElement('code')
  code.textContent = text
  pre.appendChild(code)
  container.appendChild(pre)
  document.body.appendChild(container)
  return [container, pre]
}

describe('applyCodeBlockLineNumbers', () => {
  it('counts 1 line for single line without trailing newline', () => {
    const [container, pre] = createContainerWithCode('abc')
    applyCodeBlockLineNumbers(container)
    const spans = pre.querySelectorAll('.line-numbers span')
    expect(spans.length).toBe(1)
    expect(spans[0].textContent).toBe('1')
    document.body.removeChild(container)
  })

  it('counts 1 line for single line with one trailing newline (prosemirror terminator)', () => {
    const [container, pre] = createContainerWithCode('abc\n')
    applyCodeBlockLineNumbers(container)
    const spans = pre.querySelectorAll('.line-numbers span')
    expect(spans.length).toBe(1)
    expect(spans[0].textContent).toBe('1')
    document.body.removeChild(container)
  })

  it('counts 1 line for single line with multiple trailing newlines', () => {
    const [container, pre] = createContainerWithCode('abc\n\n\n')
    applyCodeBlockLineNumbers(container)
    const spans = pre.querySelectorAll('.line-numbers span')
    expect(spans.length).toBe(1)
    document.body.removeChild(container)
  })

  it('counts 3 lines for multiline code without trailing newline', () => {
    const [container, pre] = createContainerWithCode('a\nb\nc')
    applyCodeBlockLineNumbers(container)
    const spans = pre.querySelectorAll('.line-numbers span')
    expect(spans.length).toBe(3)
    expect(spans[2].textContent).toBe('3')
    document.body.removeChild(container)
  })

  it('counts 3 lines for multiline code with trailing newline', () => {
    const [container, pre] = createContainerWithCode('a\nb\nc\n')
    applyCodeBlockLineNumbers(container)
    const spans = pre.querySelectorAll('.line-numbers span')
    expect(spans.length).toBe(3)
    expect(spans[2].textContent).toBe('3')
    document.body.removeChild(container)
  })

  it('counts 1 line for empty code block', () => {
    const [container, pre] = createContainerWithCode('')
    applyCodeBlockLineNumbers(container)
    const spans = pre.querySelectorAll('.line-numbers span')
    expect(spans.length).toBe(1)
    expect(spans[0].textContent).toBe('1')
    document.body.removeChild(container)
  })
})
