import { render } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import TipTapViewer from '@/shared/ui/TipTapViewer'
import { extractOutline } from '../extractOutline'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

const doc = {
  type: 'doc',
  content: [
    {
      type: 'heading',
      attrs: { level: 1 },
      content: [{ type: 'text', text: 'Intro' }],
    },
    { type: 'heading', attrs: { level: 2 }, content: [] },
    {
      type: 'heading',
      attrs: { level: 2 },
      content: [{ type: 'text', text: 'Setup' }],
    },
    {
      type: 'paragraph',
      content: [{ type: 'text', text: 'Body text.' }],
    },
    {
      type: 'heading',
      attrs: { level: 2 },
      content: [{ type: 'text', text: 'Setup' }],
    },
  ],
}

describe('outline/viewer heading anchor alignment', () => {
  it('extractOutline ids match the ids TipTapViewer renders, even with empty and duplicate headings', () => {
    const { container } = render(<TipTapViewer content={doc} />)

    const renderedIds = Array.from(
      container.querySelectorAll('h1, h2, h3, h4, h5, h6'),
    )
      .map((h) => h.id)
      .filter(Boolean)

    expect(renderedIds).toEqual(extractOutline(doc).map((h) => h.id))
  })
})
