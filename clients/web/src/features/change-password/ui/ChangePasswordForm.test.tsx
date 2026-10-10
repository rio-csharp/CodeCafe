import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { changePassword, useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { ApiError } from '@/shared/api'
import { ChangePasswordForm } from './ChangePasswordForm'

vi.mock('@/entities/session', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/session')>()),
  changePassword: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

/** Reveals where the success redirect landed, notice included. */
function LoginProbe() {
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice ?? ''
  return <p>{`login:${notice}`}</p>
}

function renderForm() {
  return render(
    <MemoryRouter initialEntries={['/account']}>
      <Routes>
        <Route path="/account" element={<ChangePasswordForm />} />
        <Route path="/login" element={<LoginProbe />} />
      </Routes>
    </MemoryRouter>,
  )
}

async function fillForm(user: ReturnType<typeof userEvent.setup>, confirm = 'new-password-123') {
  await user.type(screen.getByLabelText('Current password'), 'old-password')
  await user.type(screen.getByLabelText('New password'), 'new-password-123')
  await user.type(screen.getByLabelText('Repeat the new password'), confirm)
}

beforeEach(() => {
  vi.mocked(changePassword).mockReset()
  useSessionStore.setState({ status: 'authenticated', user: USER })
})

describe('ChangePasswordForm', () => {
  it('flags a confirmation mismatch before any request', async () => {
    const user = userEvent.setup()
    renderForm()
    await fillForm(user, 'something-else')

    await user.click(screen.getByRole('button', { name: 'Change password' }))

    expect(screen.getByRole('alert')).toHaveTextContent("The two passwords don't match.")
    expect(changePassword).not.toHaveBeenCalled()
  })

  it('flags a too-short new password', async () => {
    const user = userEvent.setup()
    renderForm()

    await user.type(screen.getByLabelText('Current password'), 'old-password')
    await user.type(screen.getByLabelText('New password'), 'short')
    await user.type(screen.getByLabelText('Repeat the new password'), 'short')
    await user.click(screen.getByRole('button', { name: 'Change password' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Use at least 8 characters.')
    expect(changePassword).not.toHaveBeenCalled()
  })

  it('drops the session and redirects to login with a notice on success', async () => {
    const user = userEvent.setup()
    vi.mocked(changePassword).mockResolvedValue(undefined)
    renderForm()
    await fillForm(user)

    await user.click(screen.getByRole('button', { name: 'Change password' }))

    expect(await screen.findByText('login:passwordChanged')).toBeInTheDocument()
    // The server revoked every refresh token; the local session must be gone too.
    await waitFor(() => {
      expect(useSessionStore.getState().status).toBe('anonymous')
    })
    expect(changePassword).toHaveBeenCalledWith('old-password', 'new-password-123')
  })

  it('maps incorrect_current_password to its own copy', async () => {
    const user = userEvent.setup()
    vi.mocked(changePassword).mockRejectedValue(
      new ApiError({
        status: 401,
        code: 'incorrect_current_password',
        kind: 'Unauthorized',
        message: 'nope',
      }),
    )
    renderForm()
    await fillForm(user)

    await user.click(screen.getByRole('button', { name: 'Change password' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "The current password doesn't match.",
    )
    expect(useSessionStore.getState().status).toBe('authenticated')
  })
})
