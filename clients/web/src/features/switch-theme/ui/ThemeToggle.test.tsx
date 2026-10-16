import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import i18n from '@/shared/i18n'
import { THEME_STORAGE_KEY } from '@/shared/lib'
import type { ThemeMode } from '@/shared/lib'
import { ThemeToggle } from './ThemeToggle'

type Listener = (event: MediaQueryListEvent) => void

function stubMatchMedia(matches: boolean) {
  const media = {
    matches,
    media: '(prefers-color-scheme: dark)',
    addEventListener: (_type: string, _listener: Listener) => undefined,
    removeEventListener: (_type: string, _listener: Listener) => undefined,
  }
  window.matchMedia = vi.fn(() => media) as unknown as typeof window.matchMedia
}

/** Resolved through i18next so the assertions hold in either language. */
function labelFor(mode: ThemeMode): string {
  return i18n.t('theme.labelWithMode', { mode: i18n.t(`theme.${mode}`) })
}

beforeEach(() => {
  window.localStorage.clear()
  document.documentElement.classList.remove('dark')
  stubMatchMedia(false)
})

describe('ThemeToggle', () => {
  it('renders the current mode icon and accessible name', () => {
    render(<ThemeToggle />)

    expect(screen.getByTestId('theme-icon-light')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: labelFor('light') })).toBeInTheDocument()
  })

  it('honours a stored mode on mount', () => {
    window.localStorage.setItem(THEME_STORAGE_KEY, 'dark')

    render(<ThemeToggle />)

    expect(screen.getByTestId('theme-icon-dark')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: labelFor('dark') })).toBeInTheDocument()
    expect(document.documentElement.classList.contains('dark')).toBe(true)
  })

  it('flips light ⇄ dark and persists each pick', async () => {
    const user = userEvent.setup()
    render(<ThemeToggle />)

    const toggle = () => screen.getByTestId('theme-toggle')

    await user.click(toggle())
    expect(screen.getByTestId('theme-icon-dark')).toBeInTheDocument()
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark')
    expect(document.documentElement.classList.contains('dark')).toBe(true)

    await user.click(toggle())
    expect(screen.getByTestId('theme-icon-light')).toBeInTheDocument()
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('light')
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('takes the OS theme as the initial mode while nothing is stored', () => {
    stubMatchMedia(true)

    render(<ThemeToggle />)

    expect(screen.getByTestId('theme-icon-dark')).toBeInTheDocument()
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    // Initial only: the OS preference is not a stored choice.
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBeNull()
  })
})
