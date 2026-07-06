import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  completePostAuthRedirect,
  getPostAuthRedirect,
  resolvePostAuthRedirect,
  setPostAuthRedirect,
} from './postAuthRedirect'

describe('postAuthRedirect', () => {
  beforeEach(() => {
    sessionStorage.clear()
    window.history.replaceState({}, '', '/')
  })

  afterEach(() => {
    sessionStorage.clear()
    vi.restoreAllMocks()
  })

  it('resolves same-origin relative returnUrl values', () => {
    expect(resolvePostAuthRedirect('?returnUrl=%2Fdashboard%3Ftab%3Dnotes')).toBe('/dashboard?tab=notes')
  })

  it('allows same-origin proxied MCP auth callbacks back to the API authorize endpoint', () => {
    const callbackUrl = `${window.location.origin}/connect/authorize?client_id=mcp`

    expect(
      resolvePostAuthRedirect(`?returnUrl=${encodeURIComponent(callbackUrl)}`, { apiBaseUrl: '' }),
    ).toBe(callbackUrl)
    expect(
      resolvePostAuthRedirect('?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dmcp', { apiBaseUrl: '' }),
    ).toBe(callbackUrl)
  })

  it('allows absolute MCP auth callbacks to an explicit API origin', () => {
    const callbackUrl = 'https://api.example.test/connect/authorize?client_id=mcp'

    expect(
      resolvePostAuthRedirect(`?returnUrl=${encodeURIComponent(callbackUrl)}`, {
        apiBaseUrl: 'https://api.example.test',
      }),
    ).toBe(callbackUrl)
  })

  it('rejects frontend-origin connect callbacks when the API origin is explicit', () => {
    const callbackUrl = `${window.location.origin}/connect/authorize?client_id=mcp`

    expect(
      resolvePostAuthRedirect(`?returnUrl=${encodeURIComponent(callbackUrl)}`, {
        apiBaseUrl: 'https://api.example.test',
      }),
    ).toBeNull()
  })

  it('rejects non-authorize API connect endpoints', () => {
    const tokenUrl = `${window.location.origin}/connect/token`

    expect(
      resolvePostAuthRedirect(`?returnUrl=${encodeURIComponent(tokenUrl)}`),
    ).toBeNull()
    expect(resolvePostAuthRedirect('?returnUrl=%2Fconnect%2Ftoken')).toBeNull()
  })

  it('rejects backslash-prefixed external URL smuggling', () => {
    expect(resolvePostAuthRedirect('?returnUrl=%2F%5C%5Cevil.example')).toBeNull()
    expect(resolvePostAuthRedirect(`?returnUrl=${encodeURIComponent('/\\\\evil.example')}`)).toBeNull()
  })

  it('rejects cross-origin redirects outside the API connect endpoints', () => {
    expect(
      resolvePostAuthRedirect('?returnUrl=https%3A%2F%2Fevil.example%2Fsteal'),
    ).toBeNull()
  })

  it('stores and consumes the post-auth redirect fallback', () => {
    setPostAuthRedirect('/notes/architecture-notes/edit')

    expect(getPostAuthRedirect()).toBe('/notes/architecture-notes/edit')
    expect(getPostAuthRedirect()).toBeNull()
  })

  it('falls back to stored redirect when query string does not include one', () => {
    const navigate = vi.fn()

    setPostAuthRedirect('/notes/architecture-notes/edit')
    completePostAuthRedirect('', navigate)

    expect(navigate).toHaveBeenCalledWith('/notes/architecture-notes/edit')
  })
})
