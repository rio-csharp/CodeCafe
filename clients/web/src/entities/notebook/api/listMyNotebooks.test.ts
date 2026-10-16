import { afterEach, describe, expect, it, vi } from 'vitest'
import { listMyNotebooks } from './listMyNotebooks'

function stubFetch() {
  const mock = vi.fn(async (_input: RequestInfo | URL, _init?: RequestInit) =>
    new Response(
      JSON.stringify({
        value: { items: [], page: 1, pageSize: 12, totalCount: 0, hasNextPage: false },
        error: null,
        isSuccess: true,
      }),
      { status: 200, headers: { 'content-type': 'application/json' } },
    ),
  )
  vi.stubGlobal('fetch', mock)
  return mock
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('listMyNotebooks', () => {
  it('maps favoritesOnly to the controller parameter name `favorite`', async () => {
    const fetchMock = stubFetch()

    await listMyNotebooks({ favoritesOnly: true, visibility: 'Public', page: 1 })

    const url = String(fetchMock.mock.calls[0][0])
    expect(url).toContain('favorite=true')
    expect(url).toContain('visibility=Public')
    expect(url).not.toContain('isFavorite')
  })
})
