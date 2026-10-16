import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { OverflowMenu } from './OverflowMenu'

describe('OverflowMenu', () => {
  it('stays closed until the trigger is used, then runs the picked item and closes', async () => {
    const user = userEvent.setup()
    const onSelect = vi.fn()
    render(
      <OverflowMenu
        label="More actions"
        items={[
          { id: 'first', label: 'Refresh', onSelect },
          { id: 'second', label: 'Export', onSelect: vi.fn() },
        ]}
      />,
    )

    const trigger = screen.getByRole('button', { name: 'More actions' })
    expect(trigger).toHaveAttribute('aria-expanded', 'false')
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()

    await user.click(trigger)

    expect(trigger).toHaveAttribute('aria-expanded', 'true')
    expect(screen.getByRole('menuitem', { name: 'Refresh' })).toHaveFocus()

    await user.click(screen.getByRole('menuitem', { name: 'Refresh' }))

    expect(onSelect).toHaveBeenCalledOnce()
    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('keeps the menu open for an item that confirms in place', async () => {
    const user = userEvent.setup()
    const onSelect = vi.fn()
    render(
      <OverflowMenu
        label="More actions"
        items={[{ id: 'copy', label: 'Copy link', onSelect, keepOpen: true }]}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'More actions' }))
    await user.click(screen.getByRole('menuitem', { name: 'Copy link' }))

    expect(onSelect).toHaveBeenCalledOnce()
    expect(screen.getByRole('menu')).toBeInTheDocument()
  })

  it('closes on Escape and hands focus back to the trigger', async () => {
    const user = userEvent.setup()
    render(
      <OverflowMenu label="More actions" items={[{ id: 'a', label: 'Refresh', onSelect: vi.fn() }]} />,
    )

    const trigger = screen.getByRole('button', { name: 'More actions' })
    await user.click(trigger)
    await user.keyboard('{Escape}')

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
    expect(trigger).toHaveFocus()
  })

  it('closes when the click lands outside', async () => {
    const user = userEvent.setup()
    render(
      <div>
        <OverflowMenu label="More actions" items={[{ id: 'a', label: 'Refresh', onSelect: vi.fn() }]} />
        <button type="button">Elsewhere</button>
      </div>,
    )

    await user.click(screen.getByRole('button', { name: 'More actions' }))
    await user.click(screen.getByRole('button', { name: 'Elsewhere' }))

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })

  it('moves through enabled rows with arrow keys and marks the current choice', async () => {
    const user = userEvent.setup()
    render(
      <OverflowMenu
        label="More actions"
        items={[
          { id: 'busy', label: 'Refresh', onSelect: vi.fn(), disabled: true },
          { id: 'width', label: 'Full width', onSelect: vi.fn(), selected: true },
          { id: 'export', label: 'Export', onSelect: vi.fn() },
        ]}
      />,
    )

    await user.click(screen.getByRole('button', { name: 'More actions' }))

    expect(screen.getByRole('menuitem', { name: 'Refresh' })).toBeDisabled()
    const width = screen.getByRole('menuitemcheckbox', { name: 'Full width' })
    const exportItem = screen.getByRole('menuitem', { name: 'Export' })
    expect(width).toHaveFocus()
    expect(width).toHaveAttribute('aria-checked', 'true')

    await user.keyboard('{ArrowDown}')
    expect(exportItem).toHaveFocus()
    await user.keyboard('{ArrowDown}')
    expect(width).toHaveFocus()
    await user.keyboard('{End}')
    expect(exportItem).toHaveFocus()
    await user.keyboard('{Home}')
    expect(width).toHaveFocus()
  })

  it('closes when tabbing out of the menu', async () => {
    const user = userEvent.setup()
    render(
      <OverflowMenu label="More actions" items={[{ id: 'a', label: 'Refresh', onSelect: vi.fn() }]} />,
    )

    await user.click(screen.getByRole('button', { name: 'More actions' }))
    await user.tab()

    expect(screen.queryByRole('menu')).not.toBeInTheDocument()
  })
})
