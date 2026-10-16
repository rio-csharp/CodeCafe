import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { AccountPage } from './AccountPage'

// The page is glue; the cards have their own tests.
vi.mock('@/features/update-profile', () => ({
  ProfileForm: () => <div>profile-form</div>,
}))
vi.mock('@/features/change-password', () => ({
  ChangePasswordForm: () => <div>password-form</div>,
}))
vi.mock('@/features/manage-access-tokens', () => ({
  AccessTokensCard: () => <div>tokens-card</div>,
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

/** Reveals where a redirect landed, including the `from` breadcrumb. */
function LoginProbe() {
  const location = useLocation()
  const from = (location.state as { from?: string } | null)?.from ?? ''
  return <p>{`signed-out:${from}`}</p>
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/account']}>
      <Routes>
        <Route path="/account" element={<AccountPage />} />
        <Route path="/login" element={<LoginProbe />} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  useSessionStore.setState({ status: 'authenticated', user: USER })
})

describe('AccountPage', () => {
  it('sends anonymous visitors to login with a from breadcrumb', () => {
    useSessionStore.setState({ status: 'anonymous', user: null })
    renderPage()

    expect(screen.getByText('signed-out:/account')).toBeInTheDocument()
  })

  it('renders the profile, password and token sections for a signed-in user', () => {
    renderPage()

    expect(screen.getByRole('heading', { level: 1, name: 'Account settings' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 2, name: 'Profile' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { level: 2, name: 'Change password' })).toBeInTheDocument()
    expect(
      screen.getByRole('heading', { level: 2, name: 'Personal access tokens' }),
    ).toBeInTheDocument()
    expect(screen.getByText('profile-form')).toBeInTheDocument()
    expect(screen.getByText('password-form')).toBeInTheDocument()
    expect(screen.getByText('tokens-card')).toBeInTheDocument()
  })
})
