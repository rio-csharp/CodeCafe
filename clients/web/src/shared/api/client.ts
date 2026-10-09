import { ApiError } from './errors'
import { mergeHeader, requestJson } from './request'
import type { JsonRequestInit } from './request'
import {
  clearSession,
  getAccessToken,
  getSessionVersion,
  refreshSession,
  shouldRefreshProactively,
} from './session'

export type ApiFetchInit = JsonRequestInit

/**
 * JSON-only gateway to the API, session-aware. Success returns the unwrapped
 * `value`; anything else throws {@link ApiError}. A 204 / empty body resolves
 * to `undefined`.
 */
export async function apiFetch<T>(path: string, init: ApiFetchInit = {}): Promise<T> {
  const sessionVersion = getSessionVersion()
  let authorized = withBearer(init)

  // Lazy-proactive: refresh before the server can 401 us. A refresh that failed
  // has already cleared the session, so swallow it and let the request below
  // report the real outcome.
  if (authorized.usedToken && shouldRefreshProactively()) {
    await refreshSession().catch(() => undefined)
    assertSessionUnchanged(sessionVersion)
    // Re-read the token: the refresh just replaced it.
    authorized = withBearer(init)
  }

  try {
    const value = await requestJson<T>(path, authorized.init)
    assertSessionUnchanged(sessionVersion)
    return value
  } catch (error) {
    assertSessionUnchanged(sessionVersion)

    // Anonymous requests get no second chance — there is nothing to refresh.
    if (!authorized.usedToken || !isUnauthorized(error)) {
      throw error
    }

    // The token may simply have expired: one refresh, then one retry.
    await refreshSession()
    assertSessionUnchanged(sessionVersion)

    try {
      const value = await requestJson<T>(path, withBearer(init).init)
      assertSessionUnchanged(sessionVersion)
      return value
    } catch (retryError) {
      assertSessionUnchanged(sessionVersion)

      // A second 401 is a real logout, not an expired token.
      if (isUnauthorized(retryError)) {
        clearSession()
      }
      throw retryError
    }
  }
}

interface AuthorizedRequest {
  init: JsonRequestInit
  usedToken: boolean
}

function withBearer(init: JsonRequestInit): AuthorizedRequest {
  const token = getAccessToken()
  if (token === null) {
    return { init, usedToken: false }
  }

  return {
    init: { ...init, headers: mergeHeader(init.headers, 'Authorization', `Bearer ${token}`) },
    usedToken: true,
  }
}

function isUnauthorized(error: unknown): boolean {
  return error instanceof ApiError && error.status === 401
}

function assertSessionUnchanged(expectedVersion: number): void {
  if (getSessionVersion() !== expectedVersion) {
    throw new DOMException('The session changed while the request was in flight.', 'AbortError')
  }
}

export { ApiError }
