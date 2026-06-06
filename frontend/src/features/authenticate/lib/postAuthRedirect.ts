import type { NavigateFunction } from 'react-router-dom'
import { API_BASE_URL } from '@/shared/api/client'

const ALLOWED_API_REDIRECT_PATHS = new Set(['/connect/authorize'])

interface ResolvePostAuthRedirectOptions {
  apiBaseUrl?: string
}

function hasBackslash(input: string): boolean {
  return input.includes('\\') || /%5c/i.test(input)
}

function isAllowedApiRedirectPath(pathname: string): boolean {
  return ALLOWED_API_REDIRECT_PATHS.has(pathname)
}

function isApiConnectPath(pathname: string): boolean {
  return pathname === '/connect' || pathname.startsWith('/connect/')
}

function normalizeApiOrigin(apiBaseUrl = API_BASE_URL): string | null {
  if (!apiBaseUrl) {
    return window.location.origin
  }

  try {
    return new URL(apiBaseUrl, window.location.origin).origin
  } catch {
    return null
  }
}

export function resolvePostAuthRedirect(
  search: string,
  options: ResolvePostAuthRedirectOptions = {},
): string | null {
  const params = new URLSearchParams(search)
  const returnUrl = params.get('returnUrl')
  if (!returnUrl) {
    return null
  }

  if (hasBackslash(returnUrl)) {
    return null
  }

  const apiOrigin = normalizeApiOrigin(options.apiBaseUrl)

  if (returnUrl.startsWith('/') && !returnUrl.startsWith('//')) {
    const url = new URL(returnUrl, window.location.origin)
    if (url.origin !== window.location.origin) {
      return null
    }
    if (isApiConnectPath(url.pathname) && !isAllowedApiRedirectPath(url.pathname)) {
      return null
    }
    if (isAllowedApiRedirectPath(url.pathname) && apiOrigin !== window.location.origin) {
      return null
    }
    if (isAllowedApiRedirectPath(url.pathname)) {
      return url.toString()
    }

    return `${url.pathname}${url.search}${url.hash}`
  }

  try {
    const url = new URL(returnUrl)

    if (apiOrigin && url.origin === apiOrigin && isAllowedApiRedirectPath(url.pathname)) {
      return url.toString()
    }

    if (isApiConnectPath(url.pathname)) {
      return null
    }

    if (url.origin === window.location.origin) {
      return `${url.pathname}${url.search}${url.hash}`
    }
  } catch {
    return null
  }

  return null
}

export function completePostAuthRedirect(
  search: string,
  navigate: NavigateFunction,
  fallbackPath = '/dashboard',
): void {
  const target = resolvePostAuthRedirect(search) ?? getPostAuthRedirect()
  if (!target) {
    navigate(fallbackPath)
    return
  }

  if (target.startsWith('/')) {
    navigate(target)
    return
  }

  window.location.assign(target)
}

export function setPostAuthRedirect(path: string): void {
  sessionStorage.setItem('post_auth_redirect', path)
}

export function getPostAuthRedirect(): string | null {
  const path = sessionStorage.getItem('post_auth_redirect')
  sessionStorage.removeItem('post_auth_redirect')
  return path
}
