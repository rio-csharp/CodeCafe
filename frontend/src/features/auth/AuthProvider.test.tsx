import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AuthProvider, useAuth } from './AuthProvider'
import {
  AuthenticationUnavailableError,
  getSession,
  login,
  logout,
} from './authApi'

vi.mock('./authApi', async () => {
  const actual = await vi.importActual<typeof import('./authApi')>('./authApi')

  return {
    ...actual,
    getSession: vi.fn(),
    login: vi.fn(),
    logout: vi.fn(),
  }
})

describe('AuthProvider', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('loads an authenticated session on mount', async () => {
    vi.mocked(getSession).mockResolvedValue({
      isAuthenticated: true,
      username: 'admin',
    })

    renderWithProvider(<AuthStateProbe />)

    expect(screen.getByText('status:loading')).toBeInTheDocument()
    expect(await screen.findByText('status:authenticated')).toBeInTheDocument()
    expect(screen.getByText('user:admin')).toBeInTheDocument()
  })

  it('marks auth as unavailable when session loading fails', async () => {
    vi.mocked(getSession).mockRejectedValue(new Error('boom'))

    renderWithProvider(<AuthStateProbe />)

    expect(await screen.findByText('status:unavailable')).toBeInTheDocument()
    expect(screen.getByText('message:Authentication service is unavailable right now.')).toBeInTheDocument()
  })

  it('updates state after a successful login', async () => {
    const user = userEvent.setup()
    vi.mocked(getSession).mockResolvedValue({
      isAuthenticated: false,
      username: null,
    })
    vi.mocked(login).mockResolvedValue({
      isAuthenticated: true,
      username: 'admin',
    })

    renderWithProvider(<AuthStateProbe />)
    await screen.findByText('status:unauthenticated')

    await user.click(screen.getByRole('button', { name: 'Login' }))

    expect(await screen.findByText('status:authenticated')).toBeInTheDocument()
    expect(screen.getByText('user:admin')).toBeInTheDocument()
  })

  it('keeps the current session and surfaces a message when logout fails', async () => {
    const user = userEvent.setup()
    vi.mocked(getSession).mockResolvedValue({
      isAuthenticated: true,
      username: 'admin',
    })
    vi.mocked(logout).mockRejectedValue(new AuthenticationUnavailableError('Unable to sign out right now.'))

    renderWithProvider(<AuthStateProbe />)
    await screen.findByText('status:authenticated')

    await user.click(screen.getByRole('button', { name: 'Logout' }))

    await waitFor(() => {
      expect(screen.getByText('message:Unable to sign out right now.')).toBeInTheDocument()
    })
    expect(screen.getByText('status:authenticated')).toBeInTheDocument()
    expect(screen.getByText('user:admin')).toBeInTheDocument()
  })
})

function renderWithProvider(children: ReactNode) {
  render(<AuthProvider>{children}</AuthProvider>)
}

function AuthStateProbe() {
  const auth = useAuth()

  return (
    <div>
      <div>{`status:${auth.status}`}</div>
      <div>{`user:${auth.username ?? 'none'}`}</div>
      <div>{`message:${auth.statusMessage ?? 'none'}`}</div>
      <button onClick={() => void auth.login('admin', 'secret')} type="button">
        Login
      </button>
      <button
        onClick={() => {
          void auth.logout().catch(() => undefined)
        }}
        type="button"
      >
        Logout
      </button>
    </div>
  )
}
