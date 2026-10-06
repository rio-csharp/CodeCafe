import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { login, useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { ApiError } from '@/shared/api'
import type { AuthSessionDto } from '@/shared/api'
import { LoginForm } from './LoginForm'

vi.mock('@/entities/session', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/session')>()),
  login: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const SESSION: AuthSessionDto = {
  user: USER,
  accessToken: 'access-1',
  accessTokenExpiresAtUtc: '2099-01-01T00:00:00.000Z',
  refreshToken: 'refresh-1',
}

/** The form needs a router to navigate; the extra routes prove where it landed. */
function renderForm(state?: unknown) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: '/login', state }]}>
      <Routes>
        <Route path="/login" element={<LoginForm />} />
        <Route path="/" element={<div>Home page</div>} />
        <Route path="/notebooks/espresso" element={<div>Target page</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

async function fillCredentials() {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText('Email'), 'ada@example.com')
  await user.type(screen.getByLabelText('Password'), 'espresso42')
  await user.click(screen.getByRole('button', { name: 'Sign in' }))
}

beforeEach(() => {
  vi.mocked(login).mockReset()
  useSessionStore.setState({ status: 'anonymous', user: null })
})

describe('LoginForm', () => {
  it('blocks submission and explains an invalid email', async () => {
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Email'), 'not-an-email')
    await user.type(screen.getByLabelText('Password'), 'espresso42')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByText("That email doesn't look right.")).toBeInTheDocument()
    expect(vi.mocked(login)).not.toHaveBeenCalled()
  })

  it('blocks submission when the password is empty', async () => {
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Email'), 'ada@example.com')
    await user.click(screen.getByRole('button', { name: 'Sign in' }))

    expect(await screen.findByText('Type your password.')).toBeInTheDocument()
    expect(vi.mocked(login)).not.toHaveBeenCalled()
  })

  it('posts the credentials and lands on the home page by default', async () => {
    vi.mocked(login).mockResolvedValue(SESSION)
    renderForm()

    await fillCredentials()

    expect(await screen.findByText('Home page')).toBeInTheDocument()
    expect(vi.mocked(login)).toHaveBeenCalledWith({
      email: 'ada@example.com',
      password: 'espresso42',
    })
    expect(useSessionStore.getState()).toMatchObject({ status: 'authenticated', user: USER })
  })

  it('returns to the page the visitor was sent away from', async () => {
    vi.mocked(login).mockResolvedValue(SESSION)
    renderForm({ from: '/notebooks/espresso' })

    await fillCredentials()

    expect(await screen.findByText('Target page')).toBeInTheDocument()
  })

  it('maps invalid_credentials to its own copy and keeps the password', async () => {
    vi.mocked(login).mockRejectedValue(
      new ApiError({
        status: 401,
        code: 'invalid_credentials',
        kind: 'Unauthorized',
        message: 'Invalid email or password.',
      }),
    )
    renderForm()

    await fillCredentials()

    expect(
      await screen.findByText("That email and password aren't on our list."),
    ).toBeInTheDocument()
    // A failed attempt must not punish retyping.
    expect(screen.getByLabelText('Password')).toHaveValue('espresso42')
    expect(useSessionStore.getState().status).toBe('anonymous')
  })

  it('falls back to generic copy for an unknown failure', async () => {
    vi.mocked(login).mockRejectedValue(
      new ApiError({
        status: 500,
        code: 'server.unexpected',
        kind: 'Unexpected',
        message: 'boom',
      }),
    )
    renderForm()

    await fillCredentials()

    expect(await screen.findByText('We spilled that one — try again in a moment.')).toBeInTheDocument()
  })

  it('stays put and re-enables the button while a submit is in flight', async () => {
    let release: () => void = () => undefined
    vi.mocked(login).mockImplementation(
      () =>
        new Promise<AuthSessionDto>((resolve) => {
          release = () => {
            resolve(SESSION)
          }
        }),
    )
    renderForm()

    await fillCredentials()
    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Sign in' })).toBeDisabled()
    })

    release()

    expect(await screen.findByText('Home page')).toBeInTheDocument()
  })
})
