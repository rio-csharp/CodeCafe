import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router'
import { describe, expect, it, vi } from 'vitest'
import { PagePager } from './PagePager'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

describe('PagePager', () => {
  it('links both neighbours to their reader URLs', () => {
    render(
      <MemoryRouter>
        <PagePager
          slug="brew-log"
          prev={{ path: '/grind', title: 'Grind' }}
          next={{ path: 'water/temp', title: 'Water temp' }}
        />
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: /Grind/ })).toHaveAttribute(
      'href',
      '/notebooks/brew-log/grind',
    )
    expect(screen.getByRole('link', { name: /Water temp/ })).toHaveAttribute(
      'href',
      '/notebooks/brew-log/water/temp',
    )
  })

  it('keeps the single surviving neighbour in its own slot', () => {
    render(
      <MemoryRouter>
        <PagePager slug="brew-log" prev={null} next={{ path: '/water', title: 'Water' }} />
      </MemoryRouter>,
    )

    expect(screen.getAllByRole('link')).toHaveLength(1)
    expect(screen.getByRole('link', { name: /Water/ })).toBeInTheDocument()
  })

  it('renders nothing at the notebook root, where there are no neighbours', () => {
    const { container } = render(
      <MemoryRouter>
        <PagePager slug="brew-log" prev={null} next={null} />
      </MemoryRouter>,
    )

    expect(container.querySelector('nav')).toBeNull()
    expect(screen.queryAllByRole('link')).toHaveLength(0)
  })

  it('encodes CJK and spaced path segments like the rest of the reader', () => {
    render(
      <MemoryRouter>
        <PagePager slug="咖啡笔记" prev={null} next={{ path: '/guide/深烘 notes', title: '深烘' }} />
      </MemoryRouter>,
    )

    expect(screen.getByRole('link', { name: /深烘/ })).toHaveAttribute(
      'href',
      `/notebooks/${encodeURIComponent('咖啡笔记')}/guide/${encodeURIComponent('深烘 notes')}`,
    )
  })
})
