import type { NavigateFunction } from 'react-router-dom'
import { API_BASE_URL } from '@/shared/api/client'

function normalizeApiOrigin(): string | null {
  if (!API_BASE_URL) {
    return window.location.origin
  }

  try {
    return new URL(API_BASE_URL, window.location.origin).origin
  } catch {
    return null
  }
}

export function resolvePostAuthRedirect(search: string): string | null {
  const params = new URLSearchParams(search)
  const returnUrl = params.get('returnUrl')
  if (!returnUrl) {
    return null
  }

  if (returnUrl.startsWith('/')) {
    return returnUrl
  }

  try {
    const url = new URL(returnUrl)
    const apiOrigin = normalizeApiOrigin()

    if (apiOrigin && url.origin === apiOrigin && url.pathname.startsWith('/connect/')) {
      return url.toString()
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
