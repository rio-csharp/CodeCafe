import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { updateProfile, useSessionStore } from '@/entities/session'
import type { AuthUser } from '@/entities/session'
import { ProfileForm } from './ProfileForm'

vi.mock('@/entities/session', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/session')>()),
  updateProfile: vi.fn(),
}))

const USER: AuthUser = { id: 'u1', email: 'ada@example.com', displayName: 'Ada' }

beforeEach(() => {
  vi.mocked(updateProfile).mockReset()
  useSessionStore.setState({ status: 'authenticated', user: USER })
})

describe('ProfileForm', () => {
  it('prefills the current display name', () => {
    render(<ProfileForm />)

    expect(screen.getByLabelText('Display name')).toHaveValue('Ada')
  })

  it('saves and updates the session store so the header follows without a reload', async () => {
    const user = userEvent.setup()
    vi.mocked(updateProfile).mockResolvedValue({ ...USER, displayName: 'Ada L.' })
    render(<ProfileForm />)

    const field = screen.getByLabelText('Display name')
    await user.clear(field)
    await user.type(field, 'Ada L.')
    await user.click(screen.getByRole('button', { name: 'Save name' }))

    await waitFor(() => {
      expect(useSessionStore.getState().user?.displayName).toBe('Ada L.')
    })
    expect(updateProfile).toHaveBeenCalledWith('Ada L.')
    expect(screen.getByRole('status')).toHaveTextContent('Name updated.')
  })

  it('rejects an empty name before any request', async () => {
    const user = userEvent.setup()
    render(<ProfileForm />)

    await user.clear(screen.getByLabelText('Display name'))
    await user.click(screen.getByRole('button', { name: 'Save name' }))

    expect(screen.getByRole('alert')).toHaveTextContent('Tell us what to call you.')
    expect(updateProfile).not.toHaveBeenCalled()
  })

  it('shows the failure copy when the request fails', async () => {
    const user = userEvent.setup()
    vi.mocked(updateProfile).mockRejectedValue(new Error('boom'))
    render(<ProfileForm />)

    await user.type(screen.getByLabelText('Display name'), '2')
    await user.click(screen.getByRole('button', { name: 'Save name' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Could not save. Please try again.',
    )
  })
})
