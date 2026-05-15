import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LoginPage } from './LoginPage'
import {
  AuthenticationError,
  AuthenticationRateLimitError,
  AuthenticationUnavailableError,
} from './authApi'
import { useAuth } from './useAuth'

vi.mock('./useAuth', async () => {
  const actual = await vi.importActual<typeof import('./useAuth')>('./useAuth')

  return {
    ...actual,
    useAuth: vi.fn(),
  }
})

describe('LoginPage', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('redirects authenticated users back to the requested page', async () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn(),
      status: 'authenticated',
      statusMessage: null,
      username: 'admin',
    })

    renderAtLogin({
      from: {
        pathname: '/settings',
      },
    })

    expect(await screen.findByText('Settings destination')).toBeInTheDocument()
  })

  it('shows the rate-limit message from the auth layer', async () => {
    const user = userEvent.setup()
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: false,
      login: vi.fn().mockRejectedValue(new AuthenticationRateLimitError()),
      logout: vi.fn(),
      status: 'unauthenticated',
      statusMessage: null,
      username: null,
    })

    renderAtLogin()

    await user.type(screen.getByRole('textbox', { name: 'Username' }), 'admin')
    await user.type(screen.getByLabelText('Password'), 'secret')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Too many sign-in attempts. Please wait and try again later.',
    )
  })

  it('shows invalid-credential errors from the auth layer', async () => {
    const user = userEvent.setup()
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: false,
      login: vi.fn().mockRejectedValue(new AuthenticationError()),
      logout: vi.fn(),
      status: 'unauthenticated',
      statusMessage: null,
      username: null,
    })

    renderAtLogin()

    await user.type(screen.getByRole('textbox', { name: 'Username' }), 'admin')
    await user.type(screen.getByLabelText('Password'), 'bad')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid username or password.')
  })

  it('shows auth service availability messages from the auth layer', async () => {
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: false,
      login: vi.fn().mockRejectedValue(new AuthenticationUnavailableError()),
      logout: vi.fn(),
      status: 'unavailable',
      statusMessage: 'Authentication service is unavailable right now.',
      username: null,
    })

    renderAtLogin()

    expect(screen.getByRole('alert')).toHaveTextContent('Authentication service is unavailable right now.')
  })

  it('navigates back to settings when cancel is clicked', async () => {
    const user = userEvent.setup()
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: false,
      login: vi.fn(),
      logout: vi.fn(),
      status: 'unauthenticated',
      statusMessage: null,
      username: null,
    })

    renderAtLogin()

    await user.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(await screen.findByText('Settings destination')).toBeInTheDocument()
  })
})

function renderAtLogin(state?: { from?: { pathname?: string; search?: string; hash?: string } }) {
  render(
    <MemoryRouter initialEntries={[{ pathname: '/login', state }]}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/settings" element={<div>Settings destination</div>} />
        <Route path="/" element={<div>Home destination</div>} />
      </Routes>
    </MemoryRouter>,
  )
}
