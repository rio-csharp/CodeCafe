import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  createPersonalAccessToken,
  listPersonalAccessTokens,
  revokePersonalAccessToken,
} from '@/entities/session'
import type { PersonalAccessToken } from '@/entities/session'
import { AccessTokensCard } from './AccessTokensCard'

vi.mock('@/entities/session', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/entities/session')>()),
  listPersonalAccessTokens: vi.fn(),
  createPersonalAccessToken: vi.fn(),
  revokePersonalAccessToken: vi.fn(),
}))

const ACTIVE: PersonalAccessToken = {
  id: 't1',
  name: 'CLI on my laptop',
  createdAtUtc: '2026-01-01T00:00:00Z',
  expiresAtUtc: '2026-04-01T00:00:00Z',
  revokedAtUtc: null,
}

const REVOKED: PersonalAccessToken = {
  id: 't2',
  name: 'Old script',
  createdAtUtc: '2025-06-01T00:00:00Z',
  expiresAtUtc: '2025-09-01T00:00:00Z',
  revokedAtUtc: '2025-08-01T00:00:00Z',
}

function renderCard() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(
    <QueryClientProvider client={client}>
      <AccessTokensCard />
    </QueryClientProvider>,
  )
}

beforeEach(() => {
  vi.mocked(listPersonalAccessTokens).mockReset()
  vi.mocked(createPersonalAccessToken).mockReset()
  vi.mocked(revokePersonalAccessToken).mockReset()
})

describe('AccessTokensCard', () => {
  it('lists tokens and dims the revoked ones', async () => {
    vi.mocked(listPersonalAccessTokens).mockResolvedValue([ACTIVE, REVOKED])
    renderCard()

    expect(await screen.findByText('CLI on my laptop')).toBeInTheDocument()
    expect(screen.getByText('Old script')).toBeInTheDocument()
    // Revoked rows show the badge instead of the revoke button.
    expect(screen.getByText('Revoked')).toBeInTheDocument()
    expect(screen.getAllByRole('button', { name: 'Revoke' })).toHaveLength(1)
  })

  it('shows the empty state when there are no tokens', async () => {
    vi.mocked(listPersonalAccessTokens).mockResolvedValue([])
    renderCard()

    expect(await screen.findByText('No tokens yet.')).toBeInTheDocument()
  })

  it('creates a token and reveals the raw value exactly once', async () => {
    const user = userEvent.setup()
    vi.mocked(listPersonalAccessTokens).mockResolvedValue([])
    vi.mocked(createPersonalAccessToken).mockResolvedValue({
      ...ACTIVE,
      token: 'ccpat_secret-value',
    })
    const writeText = vi.fn().mockResolvedValue(undefined)
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true })
    renderCard()

    await user.type(screen.getByLabelText('Token name'), 'CLI on my laptop')
    await user.click(screen.getByRole('button', { name: 'Create token' }))

    // The one-time reveal dialog shows the raw token.
    expect(await screen.findByText('ccpat_secret-value')).toBeInTheDocument()
    expect(screen.getByText(/only time we show it/)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Copy token' }))
    expect(writeText).toHaveBeenCalledWith('ccpat_secret-value')
    expect(await screen.findByRole('button', { name: 'Copied' })).toBeInTheDocument()

    // Closing the reveal loses the raw value for good; the list refetched.
    await user.click(screen.getByRole('button', { name: 'Done' }))
    expect(screen.queryByText('ccpat_secret-value')).not.toBeInTheDocument()
    await waitFor(() => {
      expect(listPersonalAccessTokens).toHaveBeenCalledTimes(2)
    })
  })

  it('passes the expiry in days when one is chosen', async () => {
    const user = userEvent.setup()
    vi.mocked(listPersonalAccessTokens).mockResolvedValue([])
    vi.mocked(createPersonalAccessToken).mockResolvedValue({ ...ACTIVE, token: 'ccpat_x' })
    renderCard()

    await user.type(screen.getByLabelText('Token name'), 'CLI')
    await user.type(screen.getByLabelText('Expiry in days (optional)'), '30')
    await user.click(screen.getByRole('button', { name: 'Create token' }))

    await waitFor(() => {
      expect(createPersonalAccessToken).toHaveBeenCalledWith({ name: 'CLI', expiresInDays: 30 })
    })
  })

  it('revokes after the two-step confirm', async () => {
    const user = userEvent.setup()
    vi.mocked(listPersonalAccessTokens).mockResolvedValue([ACTIVE])
    vi.mocked(revokePersonalAccessToken).mockResolvedValue(undefined)
    renderCard()

    const revokeButton = await screen.findByRole('button', { name: 'Revoke' })
    await user.click(revokeButton)
    // First click only arms the button.
    expect(revokePersonalAccessToken).not.toHaveBeenCalled()

    await user.click(screen.getByRole('button', { name: 'Revoke it?' }))
    await waitFor(() => {
      expect(revokePersonalAccessToken).toHaveBeenCalledWith('t1')
    })
  })
})
