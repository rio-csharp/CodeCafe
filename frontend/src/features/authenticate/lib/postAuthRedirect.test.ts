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
    window.__CODECAFE_CONFIG__ = {
      apiBaseUrl: `${window.location.origin}/api-proxy`,
    }
  })

  afterEach(() => {
    sessionStorage.clear()
    delete window.__CODECAFE_CONFIG__
    vi.restoreAllMocks()
  })

  it('resolves same-origin relative returnUrl values', () => {
    expect(resolvePostAuthRedirect('?returnUrl=%2Fdashboard%3Ftab%3Dnotes')).toBe('/dashboard?tab=notes')
  })

  it('allows MCP auth callbacks back to the API connect endpoints', () => {
    const callbackUrl = `${window.location.origin}/connect/authorize?client_id=mcp`

    expect(
      resolvePostAuthRedirect(`?returnUrl=${encodeURIComponent(callbackUrl)}`),
    ).toBe(callbackUrl)
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
