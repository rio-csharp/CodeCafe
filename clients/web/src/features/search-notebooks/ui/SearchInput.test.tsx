import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { SEARCH_DEBOUNCE_MS, SearchInput } from './SearchInput'

describe('SearchInput', () => {
  beforeEach(() => {
    // `shouldAdvanceTime` lets Testing Library's internal 0 ms flush settle
    // while the debounce timer itself is still under manual control.
    vi.useFakeTimers({ shouldAdvanceTime: true, advanceTimeDelta: 1 })
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('emits once for rapid typing', async () => {
    // `delay: null` keeps typing from consuming the debounce window.
    const user = userEvent.setup({ delay: null, advanceTimers: vi.advanceTimersByTime })
    const onChange = vi.fn()

    render(<SearchInput value="" onChange={onChange} />)

    await user.type(screen.getByRole('searchbox'), 'coffee')
    expect(onChange).not.toHaveBeenCalled()

    await act(async () => {
      vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS)
    })

    expect(onChange).toHaveBeenCalledTimes(1)
    expect(onChange).toHaveBeenCalledWith('coffee')
  })

  it('emits immediately when cleared', async () => {
    // `delay: null` keeps typing from consuming the debounce window.
    const user = userEvent.setup({ delay: null, advanceTimers: vi.advanceTimersByTime })
    const onChange = vi.fn()

    render(<SearchInput value="coffee" onChange={onChange} />)

    await user.click(screen.getByRole('button', { name: 'Clear' }))

    expect(onChange).toHaveBeenCalledTimes(1)
    expect(onChange).toHaveBeenCalledWith('')
  })
})
