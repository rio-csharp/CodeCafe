import {
  apiFetch,
  clearSession,
  getRefreshToken,
  onSessionCleared,
  refreshSession,
} from '@/shared/api'
import { useSessionStore } from './sessionStore'

const LOGOUT_PATH = '/api/auth/logout'

/**
 * Runs once on boot: a stored refresh token means "try to resume", anything
 * else is anonymous straight away. Returns the unsubscribe for the listener that
 * keeps the store honest when the API layer drops the session later on.
 */
export function bootstrapSession(): () => void {
  const unsubscribe = onSessionCleared(() => {
    useSessionStore.getState().setAnonymous()
  })

  if (getRefreshToken() === null) {
    useSessionStore.getState().setAnonymous()
    return unsubscribe
  }

  refreshSession()
    .then((session) => {
      const state = useSessionStore.getState()
      if (state.status === 'unknown') {
        state.setAuthenticated(session.user)
      }
    })
    .catch(() => {
      const state = useSessionStore.getState()
      if (state.status === 'unknown') {
        state.setAnonymous()
      }
    })

  return unsubscribe
}

/**
 * Revokes the refresh token server-side, but the local session goes away either
 * way — a hanging logout request must never trap the user in a signed-in UI.
 */
export function logout(): void {
  const refreshToken = getRefreshToken()

  clearSession()
  // Explicit as well as via the listener: logout has to work even when boot has
  // not subscribed yet.
  useSessionStore.getState().setAnonymous()

  if (refreshToken !== null) {
    void apiFetch(LOGOUT_PATH, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    }).catch(() => undefined)
  }
}
