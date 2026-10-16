import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it } from 'vitest'
import { useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { LoginPage } from './LoginPage'

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

function renderPage() {
  return render(
    <MemoryRouter initialEntries={['/login']}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<div>Home page</div>} />
        <Route path="/register" element={<div>Register page</div>} />
      </Routes>
    </MemoryRouter>,
  )
}

beforeEach(() => {
  useSessionStore.setState({ status: 'anonymous', user: null })
})

describe('LoginPage', () => {
  it('greets the visitor and points at registration', async () => {
    renderPage()

    expect(screen.getByRole('heading', { level: 1, name: 'Welcome back' })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'New here? Grab a cup' })).toHaveAttribute(
      'href',
      '/register',
    )
  })

  it('sends an already signed-in visitor home', async () => {
    useSessionStore.setState({ status: 'authenticated', user: USER })
    renderPage()

    expect(screen.getByText('Home page')).toBeInTheDocument()
    expect(screen.queryByRole('heading', { level: 1, name: 'Welcome back' })).not.toBeInTheDocument()
  })
})
