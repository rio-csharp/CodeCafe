import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { register as registerAccount, useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { ApiError } from '@/shared/api'
import type { AuthSessionDto } from '@/shared/api'
import { RegisterForm } from './RegisterForm'

vi.mock('@/entities/session', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/session')>()),
  register: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

const SESSION: AuthSessionDto = {
  user: USER,
  accessToken: 'access-1',
  accessTokenExpiresAtUtc: '2099-01-01T00:00:00.000Z',
  refreshToken: 'refresh-1',
}

function renderForm() {
  return render(
    <MemoryRouter initialEntries={['/register']}>
      <Routes>
        <Route path="/register" element={<RegisterForm />} />
        <Route path="/" element={<div>Home page</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

async function fillForm(password: string) {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText('Display name'), 'Ada')
  await user.type(screen.getByLabelText('Email'), 'ada@example.com')
  await user.type(screen.getByLabelText('Password'), password)
  await user.click(screen.getByRole('button', { name: 'Sign up' }))
}

beforeEach(() => {
  vi.mocked(registerAccount).mockReset()
  useSessionStore.setState({ status: 'anonymous', user: null })
})

describe('RegisterForm', () => {
  it('asks for a longer password before hitting the server', async () => {
    renderForm()

    await fillForm('short')

    expect(await screen.findByText('Use at least 8 characters.')).toBeInTheDocument()
    expect(vi.mocked(registerAccount)).not.toHaveBeenCalled()
  })

  it('requires a display name', async () => {
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Email'), 'ada@example.com')
    await user.type(screen.getByLabelText('Password'), 'espresso42')
    await user.click(screen.getByRole('button', { name: 'Sign up' }))

    expect(await screen.findByText('Tell us what to call you.')).toBeInTheDocument()
    expect(vi.mocked(registerAccount)).not.toHaveBeenCalled()
  })

  it('posts the account and lands on the home page', async () => {
    vi.mocked(registerAccount).mockResolvedValue(SESSION)
    renderForm()

    await fillForm('espresso42')

    expect(await screen.findByText('Home page')).toBeInTheDocument()
    expect(vi.mocked(registerAccount)).toHaveBeenCalledWith({
      email: 'ada@example.com',
      password: 'espresso42',
      displayName: 'Ada',
    })
    expect(useSessionStore.getState()).toMatchObject({ status: 'authenticated', user: USER })
  })

  it('maps email_already_registered to its own copy', async () => {
    vi.mocked(registerAccount).mockRejectedValue(
      new ApiError({
        status: 409,
        code: 'email_already_registered',
        kind: 'Conflict',
        message: 'A user with this email already exists.',
      }),
    )
    renderForm()

    await fillForm('espresso42')

    expect(
      await screen.findByText('That email already has a regular table here.'),
    ).toBeInTheDocument()
    expect(useSessionStore.getState().status).toBe('anonymous')
  })
})
