import { act, render, screen } from '@testing-library/react'
import { useQuery } from '@tanstack/react-query'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useSessionStore } from '@/entities/session'
import { IdentityQueryProvider } from './providers'

const USER_A = { id: 'user-a', email: 'a@example.com', displayName: 'A' }
const USER_B = { id: 'user-b', email: 'b@example.com', displayName: 'B' }

function Probe({ load }: { load: () => Promise<string> }) {
  const query = useQuery({ queryKey: ['private-page'], queryFn: load, retry: false })
  if (query.isPending) {
    return <p>loading</p>
  }
  return <p>{query.data}</p>
}

function renderProbe(load: () => Promise<string>) {
  return render(
    <IdentityQueryProvider>
      <Probe load={load} />
    </IdentityQueryProvider>,
  )
}

beforeEach(() => {
  useSessionStore.setState({ status: 'authenticated', user: USER_A })
})

describe('IdentityQueryProvider', () => {
  it('drops active private query data when the signed-in user changes', async () => {
    const load = vi
      .fn<() => Promise<string>>()
      .mockResolvedValueOnce('A private page')
      .mockResolvedValueOnce('B private page')
    renderProbe(load)
    expect(await screen.findByText('A private page')).toBeInTheDocument()

    act(() => {
      useSessionStore.setState({ status: 'anonymous', user: null })
      useSessionStore.setState({ status: 'authenticated', user: USER_B })
    })

    expect(screen.queryByText('A private page')).not.toBeInTheDocument()
    expect(await screen.findByText('B private page')).toBeInTheDocument()
    expect(load).toHaveBeenCalledTimes(2)
  })

  it('does not let a delayed response from the old identity refill the new cache', async () => {
    let resolveOld!: (value: string) => void
    const oldResponse = new Promise<string>((resolve) => {
      resolveOld = resolve
    })
    const load = vi
      .fn<() => Promise<string>>()
      .mockReturnValueOnce(oldResponse)
      .mockResolvedValueOnce('B private page')
    renderProbe(load)
    expect(screen.getByText('loading')).toBeInTheDocument()

    act(() => {
      useSessionStore.setState({ status: 'authenticated', user: USER_B })
    })
    expect(await screen.findByText('B private page')).toBeInTheDocument()

    await act(async () => {
      resolveOld('A late private page')
      await oldResponse
    })

    expect(screen.getByText('B private page')).toBeInTheDocument()
    expect(screen.queryByText('A late private page')).not.toBeInTheDocument()
  })
})

