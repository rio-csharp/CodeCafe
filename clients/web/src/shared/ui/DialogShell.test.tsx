import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { DialogShell } from './DialogShell'

describe('DialogShell keyboard accessibility', () => {
  it('does not trap Tab before native audio controls', async () => {
    const user = userEvent.setup()
    render(<DialogShell title="History" onClose={vi.fn()}><audio controls aria-label="Recording" /></DialogShell>)
    const close = screen.getByRole('button', { name: 'Close' })
    // jsdom does not make native audio controls tabbable, so assert whether
    // the trap cancels the browser's own Tab handling rather than emulate it.
    const event = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true })
    close.dispatchEvent(event)
    expect(event.defaultPrevented).toBe(false)
    await user.keyboard('{Escape}')
  })

  it('focuses the dialog, confines tab navigation, and restores the opener', async () => {
    const user = userEvent.setup()
    const opener = document.createElement('button')
    document.body.append(opener)
    opener.focus()
    const previousOverflow = document.body.style.overflow
    const { unmount } = render(<DialogShell title="Settings" onClose={vi.fn()}><input aria-label="Name" /><button>Save</button></DialogShell>)
    expect(screen.getByRole('button', { name: 'Close' })).toHaveFocus()
    await user.tab({ shift: true })
    expect(screen.getByRole('button', { name: 'Save' })).toHaveFocus()
    await user.tab()
    expect(screen.getByRole('button', { name: 'Close' })).toHaveFocus()
    unmount()
    expect(opener).toHaveFocus()
    expect(document.body.style.overflow).toBe(previousOverflow)
    opener.remove()
  })

  it('preserves current focus when the parent rerenders with a new close callback', async () => {
    const user = userEvent.setup()
    const latestClose = vi.fn()
    const { rerender } = render(<DialogShell title="Settings" onClose={vi.fn()}><input aria-label="Name" /></DialogShell>)
    await user.click(screen.getByRole('textbox'))
    rerender(<DialogShell title="Settings" onClose={latestClose}><input aria-label="Name" /></DialogShell>)
    expect(screen.getByRole('textbox')).toHaveFocus()
    await user.keyboard('{Escape}')
    expect(latestClose).toHaveBeenCalledOnce()
  })
})
