import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { MarkDto, SpanDto } from '../model/types'
import { SpanRenderer } from './SpanRenderer'

function renderSpan(text: string, marks: MarkDto[]) {
  const spans: SpanDto[] = [{ text, marks }]
  return render(<SpanRenderer spans={spans} />)
}

describe('SpanRenderer', () => {
  it('renders plain text with no marks', () => {
    render(<SpanRenderer spans={[{ text: 'brew time', marks: [] }]} />)

    expect(screen.getByText('brew time')).toBeInTheDocument()
  })

  it('renders every mark kind as its own element', () => {
    const expected: [MarkDto, string][] = [
      [{ kind: 'bold' }, 'STRONG'],
      [{ kind: 'italic' }, 'EM'],
      [{ kind: 'underline' }, 'U'],
      [{ kind: 'strike' }, 'S'],
      [{ kind: 'code' }, 'CODE'],
      [{ kind: 'kbd' }, 'KBD'],
      [{ kind: 'sup' }, 'SUP'],
      [{ kind: 'sub' }, 'SUB'],
      [{ kind: 'abbr', title: 'espresso' }, 'ABBR'],
    ]

    for (const [mark, tagName] of expected) {
      const { unmount } = renderSpan('beans', [mark])

      expect(screen.getByText('beans').tagName, mark.kind).toBe(tagName)
      unmount()
    }
  })

  it('gives the abbr mark its title', () => {
    renderSpan('ristretto', [{ kind: 'abbr', title: 'a short shot' }])

    expect(screen.getByTitle('a short shot')).toBeInTheDocument()
  })

  it('opens links in a new tab with a safe opener policy', () => {
    renderSpan('menu', [{ kind: 'link', href: 'https://example.com/menu' }])

    const link = screen.getByRole('link', { name: 'menu' })
    expect(link).toHaveAttribute('href', 'https://example.com/menu')
    expect(link).toHaveAttribute('target', '_blank')
    expect(link).toHaveAttribute('rel', 'noopener noreferrer')
  })

  it('maps colour marks onto the palette', () => {
    const { unmount } = renderSpan('done', [{ kind: 'color', name: 'success' }])
    expect(screen.getByText('done')).toHaveClass('text-success')
    unmount()

    renderSpan('stop', [{ kind: 'color', name: 'danger' }])
    expect(screen.getByText('stop')).toHaveClass('text-danger')
  })

  it('maps highlight marks onto the soft palette', () => {
    renderSpan('note', [{ kind: 'highlight', name: 'warning' }])

    expect(screen.getByText('note')).toHaveClass('bg-warning-soft')
  })

  it('composes marks by nesting them', () => {
    renderSpan('very notable', [
      { kind: 'bold' },
      { kind: 'italic' },
      { kind: 'code' },
    ])

    const innermost = screen.getByText('very notable')
    expect(innermost.tagName).toBe('CODE')
    expect(innermost.parentElement?.tagName).toBe('EM')
    expect(innermost.parentElement?.parentElement?.tagName).toBe('STRONG')
  })

  it('ignores an unknown mark instead of failing', () => {
    const unknown = { kind: 'sparkle' } as unknown as MarkDto

    renderSpan('future', [unknown, { kind: 'bold' }])

    expect(screen.getByText('future').tagName).toBe('STRONG')
  })

  it('keeps several spans in document order', () => {
    const { container } = render(
      <SpanRenderer
        spans={[
          { text: 'first', marks: [{ kind: 'bold' }] },
          { text: 'second', marks: [] },
        ]}
      />,
    )

    expect(container.textContent).toBe('firstsecond')
  })
})
