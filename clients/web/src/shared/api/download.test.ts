import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError, clearSession, setAccessCode, setSession } from '@/shared/api'
import { downloadFile } from './download'

const SESSION = {
  user: { id: 'u1', email: 'ada@example.com', displayName: 'Ada' },
  accessToken: 'token-abc',
  accessTokenExpiresAtUtc: new Date(Date.now() + 15 * 60_000).toISOString(),
  refreshToken: 'refresh-abc',
}

let clickSpy: ReturnType<typeof vi.spyOn>

function markdownResponse(markdown: string, disposition: string | null): Response {
  const headers = new Headers({ 'content-type': 'text/markdown; charset=utf-8' })
  if (disposition !== null) {
    headers.set('Content-Disposition', disposition)
  }
  return new Response(markdown, { status: 200, headers })
}

function lastDownload(): { name: string; href: string } {
  expect(clickSpy).toHaveBeenCalled()
  const anchor = clickSpy.mock.instances[0] as HTMLAnchorElement
  return { name: anchor.download, href: anchor.href }
}

beforeEach(() => {
  clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {})
  Object.defineProperty(URL, 'createObjectURL', {
    value: vi.fn(() => 'blob:mock'),
    configurable: true,
    writable: true,
  })
  Object.defineProperty(URL, 'revokeObjectURL', {
    value: vi.fn(),
    configurable: true,
    writable: true,
  })
})

afterEach(() => {
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  clearSession()
  window.sessionStorage.clear()
})

describe('downloadFile', () => {
  it('saves the blob under the Content-Disposition filename', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => markdownResponse('# Notes', 'attachment; filename="notes.md"')),
    )

    await downloadFile('/api/pages/p1/export')

    expect(lastDownload()).toEqual({ name: 'notes.md', href: 'blob:mock' })
  })

  it('prefers the RFC 5987 filename* when both forms are present', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () =>
        markdownResponse('# 咖啡', `attachment; filename="fallback.md"; filename*=UTF-8''%E5%92%96%E5%95%A1.md`),
      ),
    )

    await downloadFile('/api/notebooks/nb/export')

    expect(lastDownload().name).toBe('咖啡.md')
  })

  it('falls back to the given name when the header is missing', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => markdownResponse('# Notes', null)))

    await downloadFile('/api/pages/p1/export', { fallbackFileName: 'page.md' })

    expect(lastDownload().name).toBe('page.md')
  })

  it('attaches the bearer token and keeps caller headers', async () => {
    setSession(SESSION)
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => markdownResponse('# Notes', 'attachment; filename="n.md"')),
    )

    await downloadFile('/api/pages/p1/export', {
      headers: { 'X-CodeCafe-Access-Code': 'let-me-in' },
    })

    const [, init] = vi.mocked(fetch).mock.calls[0]
    const headers = new Headers(init?.headers)
    expect(headers.get('Authorization')).toBe('Bearer token-abc')
    expect(headers.get('X-CodeCafe-Access-Code')).toBe('let-me-in')
  })

  it('sends no Authorization header when signed out', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => markdownResponse('# Notes', 'attachment; filename="n.md"')),
    )

    await downloadFile('/api/notebooks/public-nb/export')

    const [, init] = vi.mocked(fetch).mock.calls[0]
    expect(new Headers(init?.headers).get('Authorization')).toBeNull()
  })

  it('attaches the stored access code when exporting a locked notebook', async () => {
    setAccessCode('locked-nb', 'let-me-in')
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => markdownResponse('# Notes', 'attachment; filename="n.md"')),
    )

    await downloadFile('/api/notebooks/locked-nb/export')

    const [, init] = vi.mocked(fetch).mock.calls[0]
    expect(new Headers(init?.headers).get('X-CodeCafe-Access-Code')).toBe('let-me-in')
  })

  it('lets a caller-supplied access code win over the stored one', async () => {
    setAccessCode('locked-nb', 'stale')
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => markdownResponse('# Notes', 'attachment; filename="n.md"')),
    )

    await downloadFile('/api/notebooks/locked-nb/export', {
      headers: { 'X-CodeCafe-Access-Code': 'fresh' },
    })

    const [, init] = vi.mocked(fetch).mock.calls[0]
    expect(new Headers(init?.headers).get('X-CodeCafe-Access-Code')).toBe('fresh')
  })

  it('throws the envelope error on failure instead of downloading', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(
        async () =>
          new Response(
            JSON.stringify({
              value: null,
              error: { code: 'notebook_not_found', message: 'Nope.', kind: 'NotFound' },
              isSuccess: false,
            }),
            { status: 404, headers: { 'content-type': 'application/json' } },
          ),
      ),
    )

    const failure = await downloadFile('/api/notebooks/nope/export').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ApiError)
    expect((failure as ApiError).code).toBe('notebook_not_found')
    expect(clickSpy).not.toHaveBeenCalled()
  })

  it('throws a generic ApiError when the failure body is not the envelope', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('<html>oops</html>', { status: 502 })))

    const failure = await downloadFile('/api/pages/p1/export').catch((error: unknown) => error)

    expect(failure).toBeInstanceOf(ApiError)
    expect((failure as ApiError).status).toBe(502)
    expect(clickSpy).not.toHaveBeenCalled()
  })
})
