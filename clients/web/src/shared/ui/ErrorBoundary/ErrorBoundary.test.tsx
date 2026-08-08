import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import ErrorBoundary from './ErrorBoundary'

function Bomb(): never {
  throw new Error('boom')
}

describe('ErrorBoundary', () => {
  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  it('recovers from a transient error when resetKey changes', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})

    const { rerender } = render(
      <ErrorBoundary resetKey="/a">
        <Bomb />
      </ErrorBoundary>,
    )
    expect(screen.queryByText('recovered')).toBeNull()

    rerender(
      <ErrorBoundary resetKey="/b">
        <div>recovered</div>
      </ErrorBoundary>,
    )
    expect(screen.getByText('recovered')).toBeInTheDocument()
  })

  it('keeps showing the fallback while resetKey is unchanged', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})

    const { rerender } = render(
      <ErrorBoundary resetKey="/a">
        <Bomb />
      </ErrorBoundary>,
    )

    rerender(
      <ErrorBoundary resetKey="/a">
        <div>recovered</div>
      </ErrorBoundary>,
    )
    expect(screen.queryByText('recovered')).toBeNull()
  })
})
