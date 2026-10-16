import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { BlockHandle } from './BlockHandle'

function renderHandle(overrides: Partial<Parameters<typeof BlockHandle>[0]> = {}) {
  return render(
    <BlockHandle
      canTurnInto={false}
      onTurnInto={vi.fn()}
      onDelete={vi.fn()}
      onSelect={vi.fn()}
      onDragStart={vi.fn()}
      onDragEnd={vi.fn()}
      {...overrides}
    />,
  )
}

describe('BlockHandle', () => {
  it('offers the history entry when the block has one', async () => {
    const user = userEvent.setup()
    const onShowHistory = vi.fn()
    renderHandle({ onShowHistory })

    await user.click(screen.getByRole('button', { name: 'Block menu' }))
    await user.click(screen.getByRole('menuitem', { name: 'History' }))

    expect(onShowHistory).toHaveBeenCalledTimes(1)
    // Picking the entry closes the menu.
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('hides the history entry for blocks without server-side history', async () => {
    const user = userEvent.setup()
    renderHandle()

    await user.click(screen.getByRole('button', { name: 'Block menu' }))

    expect(screen.queryByRole('menuitem', { name: 'History' })).not.toBeInTheDocument()
    expect(screen.getByRole('menuitem', { name: 'Delete' })).toBeInTheDocument()
  })
})
