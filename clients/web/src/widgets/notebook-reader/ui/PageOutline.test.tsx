import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { PageOutline } from './PageOutline'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

const HEADINGS = [
  { id: 'h-beans', text: 'Beans', level: 1 },
  { id: 'h-grind', text: 'Grind size', level: 2 },
]

/** Live heading positions, so a test can "scroll" by moving them. */
const tops: Record<string, number> = {}

function rect(top: number): DOMRect {
  return {
    top,
    bottom: top,
    left: 0,
    right: 0,
    width: 0,
    height: 0,
    x: 0,
    y: top,
    toJSON: () => ({}),
  } as DOMRect
}

/** Stands in for the rendered page: the outline only reads these by id. */
function mountHeadings(initial: Record<string, number>) {
  for (const [id, top] of Object.entries(initial)) {
    tops[id] = top
    const heading = document.createElement('h2')
    heading.id = id
    heading.getBoundingClientRect = () => rect(tops[id] ?? 0)
    document.body.append(heading)
  }
}

const scroll = () => {
  fireEvent.scroll(document)
}

const link = (name: string) => screen.getByRole('link', { name })

afterEach(() => {
  for (const id of Object.keys(tops)) {
    delete tops[id]
  }
  document.querySelectorAll('h2[id]').forEach((heading) => {
    heading.remove()
  })
})

describe('PageOutline', () => {
  it('prints the empty state without headings', () => {
    render(<PageOutline headings={[]} />)

    expect(screen.getByText('reader.noHeadings')).toBeInTheDocument()
  })

  it('lights nothing while the page still sits above its first heading', () => {
    mountHeadings({ 'h-beans': 200, 'h-grind': 420 })
    render(<PageOutline headings={HEADINGS} />)

    expect(link('Beans')).not.toHaveAttribute('aria-current')
    expect(link('Grind size')).not.toHaveAttribute('aria-current')
  })

  it('follows the scroll: the last heading past the chrome is the current section', async () => {
    mountHeadings({ 'h-beans': 200, 'h-grind': 420 })
    render(<PageOutline headings={HEADINGS} />)

    tops['h-beans'] = -120
    scroll()

    await waitFor(() => {
      expect(link('Beans')).toHaveAttribute('aria-current', 'location')
    })
    // The section stays lit even though its heading is off screen.
    expect(link('Grind size')).not.toHaveAttribute('aria-current')

    tops['h-grind'] = -20
    scroll()

    await waitFor(() => {
      expect(link('Grind size')).toHaveAttribute('aria-current', 'location')
    })
    expect(link('Beans')).not.toHaveAttribute('aria-current')
  })

  it('lights the final heading once the page cannot scroll any further', async () => {
    mountHeadings({ 'h-beans': -400, 'h-grind': 300 })

    const scroller = document.createElement('main')
    scroller.style.overflowY = 'auto'
    Object.defineProperty(scroller, 'scrollHeight', { value: 1000, configurable: true })
    Object.defineProperty(scroller, 'clientHeight', { value: 400, configurable: true })
    Object.defineProperty(scroller, 'scrollTop', { value: 600, configurable: true })
    for (const id of ['h-beans', 'h-grind']) {
      scroller.append(document.getElementById(id)!)
    }
    document.body.append(scroller)

    try {
      render(<PageOutline headings={HEADINGS} />)
      scroll()

      await waitFor(() => {
        expect(link('Grind size')).toHaveAttribute('aria-current', 'location')
      })
    } finally {
      scroller.remove()
    }
  })
})
