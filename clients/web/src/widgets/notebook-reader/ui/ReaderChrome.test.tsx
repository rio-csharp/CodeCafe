import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ReaderChrome } from './ReaderChrome'
import { RightPanel } from './RightPanel'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

describe('RightPanel', () => {
  const tabs = [
    { id: 'outline', label: 'Outline', icon: <path d="M2 4h12" />, content: <p>outline body</p> },
    { id: 'chat', label: 'Chat', icon: <path d="M2 8h12" />, content: <p>chat body</p> },
  ]

  it('opens on the first tab and swaps bodies when another tab is picked', async () => {
    const user = userEvent.setup()
    render(<RightPanel tabs={tabs} />)

    expect(screen.getByText('outline body')).toBeInTheDocument()
    expect(screen.queryByText('chat body')).not.toBeInTheDocument()

    await user.click(screen.getByRole('tab', { name: 'Chat' }))

    expect(screen.getByText('chat body')).toBeInTheDocument()
    expect(screen.queryByText('outline body')).not.toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Chat' })).toHaveAttribute('aria-selected', 'true')
  })

  it('associates tabs with the active panel and supports standard keyboard navigation', async () => {
    const user = userEvent.setup()
    render(<RightPanel tabs={tabs} />)

    const outline = screen.getByRole('tab', { name: 'Outline' })
    const chat = screen.getByRole('tab', { name: 'Chat' })
    const panel = screen.getByRole('tabpanel')
    expect(outline).toHaveAttribute('aria-controls', panel.id)
    expect(panel).toHaveAttribute('aria-labelledby', outline.id)
    expect(outline).toHaveAttribute('tabindex', '0')
    expect(chat).toHaveAttribute('tabindex', '-1')

    outline.focus()
    await user.keyboard('{ArrowLeft}')
    expect(chat).toHaveFocus()
    expect(chat).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tabpanel')).toHaveAttribute('aria-labelledby', chat.id)

    await user.keyboard('{Home}')
    expect(outline).toHaveFocus()
    await user.keyboard('{End}')
    expect(chat).toHaveFocus()
    await user.keyboard('{ArrowRight}')
    expect(outline).toHaveFocus()
  })

  it('renders nothing without tabs', () => {
    const { container } = render(<RightPanel tabs={[]} />)
    expect(container).toBeEmptyDOMElement()
  })
})

describe('ReaderChrome', () => {
  function renderChrome(overrides: Partial<Parameters<typeof ReaderChrome>[0]> = {}) {
    return render(
      <ReaderChrome
        title="Grinding"
        refreshing={false}
        onRefresh={vi.fn()}
        wide
        onToggleWide={vi.fn()}
        {...overrides}
      />,
    )
  }

  it('forwards the refresh request from the overflow menu', async () => {
    const user = userEvent.setup()
    const onRefresh = vi.fn()
    renderChrome({ onRefresh })

    await user.click(screen.getByRole('button', { name: 'reader.moreActions' }))
    await user.click(screen.getByRole('menuitem', { name: 'reader.refresh' }))
    expect(onRefresh).toHaveBeenCalledOnce()
  })

  it('keeps only the edit pill beside the title and folds the rest away', () => {
    renderChrome({ canEdit: true, onEdit: vi.fn(), onExportPage: vi.fn(), onSharePage: vi.fn() })

    expect(screen.getByRole('button', { name: 'reader.moreActions' })).toHaveAttribute(
      'aria-expanded',
      'false',
    )
    expect(screen.queryByRole('menuitem', { name: 'reader.refresh' })).not.toBeInTheDocument()
  })

  it('copies the current URL and confirms briefly', async () => {
    const user = userEvent.setup()
    const writeText = vi.fn().mockResolvedValue(undefined)
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true })
    renderChrome()

    await user.click(screen.getByRole('button', { name: 'reader.moreActions' }))
    await user.click(screen.getByRole('menuitem', { name: 'reader.copyLink' }))

    expect(writeText).toHaveBeenCalledWith(window.location.href)
    // The row confirms in place, so the menu stays open.
    expect(await screen.findByRole('menuitem', { name: 'reader.linkCopied' })).toBeInTheDocument()
  })

  it('shows a retryable failure when clipboard access is rejected', async () => {
    const user = userEvent.setup()
    const writeText = vi.fn().mockRejectedValue(new DOMException('denied', 'NotAllowedError'))
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true })
    renderChrome()

    await user.click(screen.getByRole('button', { name: 'reader.moreActions' }))
    await user.click(screen.getByRole('menuitem', { name: 'reader.copyLink' }))

    await waitFor(() => {
      expect(writeText).toHaveBeenCalledWith(window.location.href)
    })
    expect(screen.getByRole('menuitem', { name: 'reader.copyFailed' })).toBeInTheDocument()
  })

  it('shows a retryable failure when the Clipboard API is unavailable', async () => {
    const user = userEvent.setup()
    Object.defineProperty(navigator, 'clipboard', { value: undefined, configurable: true })
    renderChrome()

    await user.click(screen.getByRole('button', { name: 'reader.moreActions' }))
    await user.click(screen.getByRole('menuitem', { name: 'reader.copyLink' }))

    expect(await screen.findByRole('menuitem', { name: 'reader.copyFailed' })).toBeInTheDocument()
  })

  it('hides the edit pill from readers and offers it to writers', async () => {
    const user = userEvent.setup()
    const onEdit = vi.fn()
    const { unmount } = renderChrome()

    expect(screen.queryByRole('button', { name: 'reader.edit' })).not.toBeInTheDocument()

    unmount()
    renderChrome({ canEdit: true, onEdit })
    await user.click(screen.getByRole('button', { name: 'reader.edit' }))

    expect(onEdit).toHaveBeenCalledOnce()
  })

  it('renders the favorite action slot and forwards the export request', async () => {
    const user = userEvent.setup()
    const onExportPage = vi.fn()
    renderChrome({ favoriteAction: <span data-testid="star" />, onExportPage })

    expect(screen.getByTestId('star')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'reader.moreActions' }))
    await user.click(screen.getByRole('menuitem', { name: 'reader.exportPage' }))
    expect(onExportPage).toHaveBeenCalledOnce()
  })

  it('omits the export row without an export handler', async () => {
    const user = userEvent.setup()
    renderChrome()

    await user.click(screen.getByRole('button', { name: 'reader.moreActions' }))

    expect(screen.queryByRole('menuitem', { name: 'reader.exportPage' })).not.toBeInTheDocument()
  })

  it('publishes the sticky bar height so jumped-to headings clear it', () => {
    const { unmount } = renderChrome()

    // jsdom reports a zero-height box, so this is the gap alone — enough to
    // prove the offset is positive and in pixels.
    expect(document.documentElement.style.getPropertyValue('--reader-chrome-offset')).toBe('16px')

    unmount()
    expect(document.documentElement.style.getPropertyValue('--reader-chrome-offset')).toBe('')
  })

  it('marks the width preference and toggles it from the menu', async () => {
    const user = userEvent.setup()
    const onToggleWide = vi.fn()
    renderChrome({ wide: true, onToggleWide })

    await user.click(screen.getByRole('button', { name: 'reader.moreActions' }))
    await user.click(screen.getByRole('menuitemcheckbox', { name: 'reader.narrowWidth' }))

    expect(onToggleWide).toHaveBeenCalledOnce()
    // Picking an action closes the menu again.
    expect(screen.queryByRole('menuitemcheckbox', { name: 'reader.narrowWidth' })).not.toBeInTheDocument()
  })
})
