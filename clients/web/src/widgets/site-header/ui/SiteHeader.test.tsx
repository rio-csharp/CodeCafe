import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { REFRESH_STORAGE_KEY } from '@/shared/api'
import { SiteHeader } from './SiteHeader'

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

function renderHeader(path = '/notebooks/espresso') {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <SiteHeader />
    </MemoryRouter>,
  )
}

beforeEach(() => {
  window.localStorage.clear()
  useSessionStore.setState({ status: 'anonymous', user: null })
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('SiteHeader', () => {
  it('offers a login link that remembers where the visitor was', async () => {
    renderHeader()

    const link = screen.getByRole('link', { name: 'Log in' })
    expect(link).toHaveAttribute('href', '/login')
    expect(screen.queryByRole('button', { name: 'Log out' })).not.toBeInTheDocument()
  })

  it('renders neither state while the session is unknown', async () => {
    useSessionStore.setState({ status: 'unknown', user: null })
    renderHeader()

    expect(screen.queryByRole('link', { name: 'Log in' })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Log out' })).not.toBeInTheDocument()
  })

  it('shows the display name as a menu trigger when signed in', async () => {
    useSessionStore.setState({ status: 'authenticated', user: USER })
    renderHeader()

    const trigger = screen.getByRole('button', { name: 'Ada' })
    expect(trigger).toHaveAttribute('aria-haspopup', 'menu')
    expect(trigger).toHaveAttribute('aria-expanded', 'false')
    // The way out lives inside the menu, not on the bar itself.
    expect(screen.queryByRole('menuitem', { name: 'Log out' })).not.toBeInTheDocument()
    expect(screen.queryByRole('link', { name: 'Log in' })).not.toBeInTheDocument()
  })

  it('opens the account menu on click and closes it on Escape', async () => {
    const user = userEvent.setup()
    useSessionStore.setState({ status: 'authenticated', user: USER })
    renderHeader()

    await user.click(screen.getByRole('button', { name: 'Ada' }))
    expect(screen.getByRole('menuitem', { name: 'Log out' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Ada' })).toHaveAttribute('aria-expanded', 'true')

    await user.keyboard('{Escape}')
    expect(screen.queryByRole('menuitem', { name: 'Log out' })).not.toBeInTheDocument()
  })

  it('clears the session on logout without leaving the page', async () => {
    const user = userEvent.setup()
    const fetchMock = vi.fn(
      async (_input: RequestInfo | URL, _init?: RequestInit) =>
        new Response(JSON.stringify({ value: null, error: null, isSuccess: true }), {
          status: 200,
          headers: { 'content-type': 'application/json' },
        }),
    )
    vi.stubGlobal('fetch', fetchMock)
    window.localStorage.setItem(REFRESH_STORAGE_KEY, 'refresh-1')
    useSessionStore.setState({ status: 'authenticated', user: USER })
    renderHeader()

    await user.click(screen.getByRole('button', { name: 'Ada' }))
    await user.click(screen.getByRole('menuitem', { name: 'Log out' }))

    expect(useSessionStore.getState()).toMatchObject({ status: 'anonymous', user: null })
    expect(window.localStorage.getItem(REFRESH_STORAGE_KEY)).toBeNull()
    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledTimes(1)
    })
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/auth/logout')
    expect(JSON.parse(String(fetchMock.mock.calls[0]?.[1]?.body))).toEqual({
      refreshToken: 'refresh-1',
    })
    expect(screen.getByRole('link', { name: 'Log in' })).toBeInTheDocument()
  })
})
