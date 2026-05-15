import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { SettingsPage } from './SettingsPage'
import { useAuth } from '../auth/AuthProvider'
import { useTheme } from '../../app/useTheme'
import { isLocalEnvironment } from '../../app/runtimeEnvironment'
import { getNotesSettings, updateNotesSettings } from './notesSettingsApi'

vi.mock('../auth/AuthProvider', () => ({
  useAuth: vi.fn(),
}))

vi.mock('../../app/useTheme', () => ({
  useTheme: vi.fn(),
}))

vi.mock('../../app/runtimeEnvironment', () => ({
  isLocalEnvironment: vi.fn(),
}))

vi.mock('./notesSettingsApi', () => ({
  getNotesSettings: vi.fn(),
  updateNotesSettings: vi.fn(),
}))

describe('SettingsPage', () => {
  afterEach(() => {
    vi.clearAllMocks()
  })

  it('shows the authentication service state when auth is unavailable', () => {
    mockTheme()
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: false,
      login: vi.fn(),
      logout: vi.fn(),
      status: 'unavailable',
      statusMessage: 'Authentication service is unavailable right now.',
      username: null,
    })

    renderPage()

    expect(screen.getByText('Authentication service is unavailable right now.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeDisabled()
  })

  it('saves notes settings in local authenticated environments', async () => {
    const user = userEvent.setup()
    mockTheme()
    vi.mocked(isLocalEnvironment).mockReturnValue(true)
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn(),
      status: 'authenticated',
      statusMessage: null,
      username: 'admin',
    })
    vi.mocked(getNotesSettings).mockResolvedValue({ rootPath: '/srv/codecafe/notes' })
    vi.mocked(updateNotesSettings).mockResolvedValue({ rootPath: '/srv/new-notes' })

    renderPage()

    const input = await screen.findByRole('textbox', { name: 'Notes root path' })
    await user.clear(input)
    await user.type(input, '/srv/new-notes')
    await user.click(screen.getByRole('button', { name: 'Save' }))

    await waitFor(() => {
      expect(updateNotesSettings).toHaveBeenCalledWith({ rootPath: '/srv/new-notes' })
    })
    expect(screen.getByText('Notes settings saved.')).toBeInTheDocument()
    expect(screen.getByDisplayValue('/srv/new-notes')).toBeInTheDocument()
  })

  it('shows a save failure message when updating notes settings fails', async () => {
    const user = userEvent.setup()
    mockTheme()
    vi.mocked(isLocalEnvironment).mockReturnValue(true)
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn(),
      status: 'authenticated',
      statusMessage: null,
      username: 'admin',
    })
    vi.mocked(getNotesSettings).mockResolvedValue({ rootPath: '/srv/codecafe/notes' })
    vi.mocked(updateNotesSettings).mockRejectedValue(new Error('boom'))

    renderPage()

    await screen.findByRole('textbox', { name: 'Notes root path' })
    await user.click(screen.getByRole('button', { name: 'Save' }))

    expect(await screen.findByText('Save failed.')).toBeInTheDocument()
  })

  it('shows a sign-out failure message without clearing the page', async () => {
    const user = userEvent.setup()
    mockTheme()
    vi.mocked(isLocalEnvironment).mockReturnValue(false)
    vi.mocked(useAuth).mockReturnValue({
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn().mockRejectedValue(new Error('boom')),
      status: 'authenticated',
      statusMessage: null,
      username: 'admin',
    })
    vi.mocked(getNotesSettings).mockResolvedValue({ rootPath: '/srv/codecafe/notes' })

    renderPage()

    await user.click(await screen.findByRole('button', { name: 'Sign out' }))

    expect(await screen.findByText('Unable to sign out right now.')).toBeInTheDocument()
    expect(screen.getByText('admin')).toBeInTheDocument()
  })
})

function renderPage() {
  render(
    <MemoryRouter>
      <SettingsPage />
    </MemoryRouter>,
  )
}

function mockTheme() {
  vi.mocked(useTheme).mockReturnValue({
    setTheme: vi.fn(),
    theme: 'dark',
  })
}
