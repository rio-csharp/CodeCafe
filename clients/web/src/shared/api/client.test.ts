import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, apiFetch } from './client'

function jsonResponse(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('apiFetch', () => {
  it('unwraps the success envelope', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        jsonResponse({ value: { items: [1, 2] }, error: null, isSuccess: true }, 200),
      ),
    )

    await expect(apiFetch<{ items: number[] }>('/api/notebooks')).resolves.toEqual({ items: [1, 2] })
  })

  it('resolves to undefined for an empty body', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response(null, { status: 204 })))

    await expect(apiFetch<void>('/api/notebooks')).resolves.toBeUndefined()
  })

  it('throws ApiError with status, code and kind on a failure envelope', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        jsonResponse(
          {
            value: null,
            error: {
              code: 'notebooks.search_invalid',
              message: 'Search term is too long.',
              kind: 'Validation',
            },
            isSuccess: false,
          },
          400,
        ),
      ),
    )

    const error = await apiFetch('/api/notebooks').catch((cause: unknown) => cause)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      code: 'notebooks.search_invalid',
      kind: 'Validation',
      message: 'Search term is too long.',
    })
  })

  it('throws when an error response is not valid JSON', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => new Response('<html>boom</html>', { status: 500 })),
    )

    const error = await apiFetch('/api/notebooks').catch((cause: unknown) => cause)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 500,
      code: 'client.malformed_response',
      kind: 'Unexpected',
    })
  })
})
