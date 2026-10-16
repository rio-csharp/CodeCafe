import { ApiError } from './errors'
import { requestJson } from './request'
import type { AuthSessionDto } from './types'

/** The third localStorage key the app owns, after `codecafe.lang` and `codecafe.theme`. */
export const REFRESH_STORAGE_KEY = 'codecafe.refresh'

const REFRESH_PATH = '/api/auth/refresh'

const INVALID_REFRESH_TOKEN = 'invalid_refresh_token'

/** Refresh this close to expiry so the common path never sees a 401. */
export const PROACTIVE_REFRESH_WINDOW_MS = 60_000

export interface SessionClock {
  /** Epoch milliseconds. */
  now(): number
}

const systemClock: SessionClock = { now: () => Date.now() }

let clock: SessionClock = systemClock

/** Test seam: replaces the clock the expiry check reads. Pass `null` to restore. */
export function setSessionClock(next: SessionClock | null): void {
  clock = next ?? systemClock
}

let accessToken: string | null = null
let accessTokenExpiresAtMs: number | null = null
// Invalidates an in-flight refresh when logout (or a newer login) changes the
// local session before the refresh response arrives.
let sessionVersion = 0
let refreshInFlight: { version: number; promise: Promise<AuthSessionDto> } | null = null

const clearedListeners = new Set<() => void>()

export function getAccessToken(): string | null {
  return accessToken
}

/** Opaque identity snapshot for rejecting work that outlives its session. */
export function getSessionVersion(): number {
  return sessionVersion
}

/** The refresh token lives in localStorage only — never in memory, never in a cookie. */
export function getRefreshToken(): string | null {
  try {
    return window.localStorage.getItem(REFRESH_STORAGE_KEY)
  } catch {
    // Private mode / disabled storage: behave as if there is no session.
    return null
  }
}

/** Stores both tokens of a freshly issued session; refresh tokens always rotate. */
export function setSession(dto: AuthSessionDto): void {
  sessionVersion += 1
  commitSession(dto)
}

function commitSession(dto: AuthSessionDto): void {
  accessToken = dto.accessToken
  const expiresAtMs = Date.parse(dto.accessTokenExpiresAtUtc)
  accessTokenExpiresAtMs = Number.isNaN(expiresAtMs) ? null : expiresAtMs
  writeRefreshToken(dto.refreshToken)
}

export function clearSession(): void {
  sessionVersion += 1
  accessToken = null
  accessTokenExpiresAtMs = null
  removeRefreshToken()

  for (const listener of [...clearedListeners]) {
    try {
      listener()
    } catch {
      // One broken listener must not stop the others from hearing the news.
    }
  }
}

/** Fires whenever the session goes away locally, including a failed refresh. */
export function onSessionCleared(listener: () => void): () => void {
  clearedListeners.add(listener)
  return () => {
    clearedListeners.delete(listener)
  }
}

/** True when the access token is missing or about to expire. */
export function shouldRefreshProactively(): boolean {
  if (accessToken === null || accessTokenExpiresAtMs === null) {
    return false
  }
  return accessTokenExpiresAtMs - clock.now() <= PROACTIVE_REFRESH_WINDOW_MS
}

/**
 * Exchanges the refresh token for a new session. Concurrent callers share one
 * request; a 401 clears the session and rejects every waiter.
 */
export function refreshSession(): Promise<AuthSessionDto> {
  if (refreshInFlight?.version === sessionVersion) {
    return refreshInFlight.promise
  }

  const refreshToken = getRefreshToken()
  if (refreshToken === null) {
    return Promise.reject(
      new ApiError({
        status: 401,
        code: INVALID_REFRESH_TOKEN,
        kind: 'Unauthorized',
        message: 'There is no refresh token to exchange.',
      }),
    )
  }

  const versionAtStart = sessionVersion

  const attempt = requestJson<AuthSessionDto>(REFRESH_PATH, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
    .then((dto) => {
      if (sessionVersion !== versionAtStart) {
        throw new DOMException('The session changed while it was refreshing.', 'AbortError')
      }
      commitSession(dto)
      return dto
    })
    .catch((error: unknown) => {
      if (sessionVersion === versionAtStart && isInvalidRefreshToken(error)) {
        clearSession()
      }
      throw error
    })
    .finally(() => {
      if (refreshInFlight?.promise === attempt) {
        refreshInFlight = null
      }
    })

  refreshInFlight = { version: versionAtStart, promise: attempt }
  return attempt
}

function isInvalidRefreshToken(error: unknown): boolean {
  return error instanceof ApiError && (error.code === INVALID_REFRESH_TOKEN || error.status === 401)
}

function writeRefreshToken(token: string): void {
  try {
    window.localStorage.setItem(REFRESH_STORAGE_KEY, token)
  } catch {
    // Storage is unavailable: the session works for this page view only.
  }
}

function removeRefreshToken(): void {
  try {
    window.localStorage.removeItem(REFRESH_STORAGE_KEY)
  } catch {
    // Nothing to clear if storage is unavailable.
  }
}
