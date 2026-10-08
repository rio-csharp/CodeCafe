import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { BlockDto } from '../model/types'
import type { BlockNode } from '../model/assembleBlockTree'
import { BlockList, BlockRenderer } from './BlockRenderer'

const text = (value: string) => [{ text: value, marks: [] }]

function renderBlock(block: Partial<BlockDto>) {
  const full: BlockDto = {
    id: 'block-1',
    parentBlockId: null,
    type: 'paragraph',
    content: {},
    sortKey: 'a',
    version: 1,
    updatedAtUtc: '2026-01-07T12:00:00.000Z',
    ...block,
  }
  const node: BlockNode = { block: full, children: [] }

  return render(<BlockRenderer node={node} />)
}

describe('BlockRenderer', () => {
  it('renders a paragraph', () => {
    renderBlock({ type: 'paragraph', content: { spans: text('Freshly brewed.') } })

    expect(screen.getByText('Freshly brewed.').tagName).toBe('P')
  })

  it('renders a heading at its own level', () => {
    renderBlock({ type: 'heading', content: { level: 2, spans: text('Grind size') } })

    expect(screen.getByRole('heading', { level: 2, name: 'Grind size' })).toBeInTheDocument()
  })

  it('clamps an out-of-range heading level instead of inventing a tag', () => {
    renderBlock({ type: 'heading', content: { level: 9, spans: text('Too deep') } })

    expect(screen.getByRole('heading', { level: 6, name: 'Too deep' })).toBeInTheDocument()
  })

  it('renders a quote', () => {
    renderBlock({ type: 'quote', content: { spans: text('Espresso is patience.') } })

    expect(screen.getByText('Espresso is patience.').tagName).toBe('BLOCKQUOTE')
  })

  it('renders a callout variant', () => {
    renderBlock({
      type: 'callout',
      content: { variant: 'warning', spans: text('Careful, it is hot.') },
    })

    const callout = screen.getByRole('complementary')
    expect(callout).toHaveTextContent('Careful, it is hot.')
    expect(callout).toHaveClass('bg-warning-soft')
  })

  it('renders a todo as a read-only checkbox', () => {
    renderBlock({ type: 'todo', content: { checked: true, spans: text('Buy beans') } })

    const checkbox = screen.getByRole('checkbox')
    expect(checkbox).toBeChecked()
    expect(checkbox).toBeDisabled()
    expect(screen.getByText('Buy beans')).toHaveClass('line-through')
  })

  it('renders code as a preformatted block', () => {
    renderBlock({
      type: 'code',
      content: { code: 'npm run brew', language: 'bash' },
    })

    const sample = screen.getByText('npm run brew')
    expect(sample.tagName).toBe('CODE')
    expect(sample.parentElement?.tagName).toBe('PRE')
  })

  it('renders a divider', () => {
    renderBlock({ type: 'divider', content: {} })

    expect(screen.getByRole('separator')).toBeInTheDocument()
  })

  it('renders a real table with its header and rows', () => {
    renderBlock({
      type: 'table',
      content: {
        alignments: ['left', 'right'],
        header: [text('Origin'), text('Price')],
        rows: [[text('Ethiopia'), text('42')]],
      },
    })

    const table = screen.getByRole('table')
    expect(table).toBeInTheDocument()
    expect(screen.getAllByRole('columnheader').map((cell) => cell.textContent)).toEqual([
      'Origin',
      'Price',
    ])
    expect(screen.getAllByRole('row')).toHaveLength(2)
    expect(screen.getByRole('cell', { name: 'Ethiopia' })).toBeInTheDocument()
  })

  it('aligns table columns per column', () => {
    renderBlock({
      type: 'table',
      content: {
        alignments: ['center'],
        header: null,
        rows: [[text('middle')]],
      },
    })

    expect(screen.queryByRole('columnheader')).not.toBeInTheDocument()
    expect(screen.getByRole('cell', { name: 'middle' })).toHaveClass('text-center')
  })

  it('renders a lazy image with its alt text', () => {
    renderBlock({
      type: 'image',
      content: { url: 'https://example.com/cup.png', alt: 'A cup', isDecorative: false, caption: null },
    })

    const image = screen.getByAltText('A cup')
    expect(image).toHaveAttribute('src', 'https://example.com/cup.png')
    expect(image).toHaveAttribute('loading', 'lazy')
  })

  it('renders audio with native controls', () => {
    const { container } = renderBlock({
      type: 'audio',
      content: { url: 'https://example.com/pour.mp3', mimeType: 'audio/mpeg', duration: null },
    })

    const audio = container.querySelector('audio')
    expect(audio).not.toBeNull()
    expect(audio).toHaveAttribute('controls')
    expect(audio?.querySelector('source')).toHaveAttribute('type', 'audio/mpeg')
  })

  it('renders nothing for an unknown type but warns in development', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined)
    const { container } = renderBlock({ type: 'hologram', content: { spans: [] } })

    expect(container).toBeEmptyDOMElement()
    expect(warn).toHaveBeenCalledWith(expect.stringContaining('hologram'))
    warn.mockRestore()
  })

  it('nests a child block under its parent', () => {
    const parent: BlockNode = {
      block: {
        id: 'parent',
        parentBlockId: null,
        type: 'paragraph',
        content: { spans: text('Parent') },
        sortKey: 'a',
        version: 1,
        updatedAtUtc: '2026-01-07T12:00:00.000Z',
      },
      children: [
        {
          block: {
            id: 'child',
            parentBlockId: 'parent',
            type: 'paragraph',
            content: { spans: text('Child') },
            sortKey: 'a',
            version: 1,
            updatedAtUtc: '2026-01-07T12:00:00.000Z',
          },
          children: [],
        },
      ],
    }

    render(<BlockRenderer node={parent} />)

    const child = screen.getByText('Child')
    expect(child.parentElement?.parentElement).toContainElement(screen.getByText('Parent'))
  })

  it('renders a callout\'s children inside the box', () => {
    const callout: BlockNode = {
      block: {
        id: 'callout-1',
        parentBlockId: null,
        type: 'callout',
        content: { variant: 'info', spans: text('Heads up') },
        sortKey: 'a',
        version: 1,
        updatedAtUtc: '2026-01-07T12:00:00.000Z',
      },
      children: [
        {
          block: {
            id: 'child-1',
            parentBlockId: 'callout-1',
            type: 'bulleted-list',
            content: { spans: text('Inside') },
            sortKey: 'a',
            version: 1,
            updatedAtUtc: '2026-01-07T12:00:00.000Z',
          },
          children: [],
        },
      ],
    }

    render(<BlockRenderer node={callout} />)

    const box = screen.getByRole('complementary')
    expect(box).toHaveTextContent('Heads up')
    expect(box).toContainElement(screen.getByText('Inside'))
  })

  it('renders a code block with syntax highlighting', () => {
    renderBlock({
      type: 'code',
      content: { code: 'const x = 1;', language: 'javascript' },
    })

    expect(screen.getByText('const')).toHaveClass('hljs-keyword')
  })

  it('shows the code language label, and hides it when unset', () => {
    renderBlock({
      type: 'code',
      content: { code: 'const x = 1;', language: 'javascript' },
    })
    expect(screen.getByText('javascript')).toBeInTheDocument()
  })

  it('hides the language label when the language is blank', () => {
    const { container } = renderBlock({ type: 'code', content: { code: 'plain', language: '  ' } })

    expect(container.textContent).toBe('plain')
  })

  it('renders a whole list of blocks', () => {
    const nodes: BlockNode[] = ['one', 'two'].map((value, index) => ({
      block: {
        id: `block-${index}`,
        parentBlockId: null,
        type: 'paragraph',
        content: { spans: text(value) },
        sortKey: String(index),
        version: 1,
        updatedAtUtc: '2026-01-07T12:00:00.000Z',
      },
      children: [],
    }))

    render(<BlockList nodes={nodes} />)

    expect(screen.getByText('one')).toBeInTheDocument()
    expect(screen.getByText('two')).toBeInTheDocument()
  })

  it('numbers consecutive ordered items and restarts after a break', () => {
    const make = (id: string, type: string, value: string): BlockNode => ({
      block: {
        id,
        parentBlockId: null,
        type,
        content: { spans: text(value) },
        sortKey: id,
        version: 1,
        updatedAtUtc: '2026-01-07T12:00:00.000Z',
      },
      children: [],
    })

    render(
      <BlockList
        nodes={[
          make('a', 'numbered-list', 'first'),
          make('b', 'numbered-list', 'second'),
          make('c', 'paragraph', 'break'),
          make('d', 'numbered-list', 'restarted'),
        ]}
      />,
    )

    expect(screen.getByText('first').closest('div')?.parentElement).toHaveTextContent('1.')
    expect(screen.getByText('second').closest('div')?.parentElement).toHaveTextContent('2.')
    expect(screen.getByText('restarted').closest('div')?.parentElement).toHaveTextContent('1.')
  })
})
