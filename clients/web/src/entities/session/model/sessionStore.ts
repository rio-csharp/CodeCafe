import { create } from 'zustand'
import type { AuthUserDto } from '@/shared/api'

/**
 * `unknown` means "there may be a session, we have not asked yet" — the UI
 * renders neither the login button nor the user so nothing flashes wrong.
 */
export type SessionStatus = 'unknown' | 'authenticated' | 'anonymous'

export interface SessionState {
  status: SessionStatus
  user: AuthUserDto | null
  setAuthenticated: (user: AuthUserDto) => void
  setAnonymous: () => void
}

/**
 * The only client state in M3: the header, the homepage section and the two auth
 * pages all need it, which is past the "lift it into a page" threshold.
 */
export const useSessionStore = create<SessionState>()((set) => ({
  status: 'unknown',
  user: null,
  setAuthenticated: (user) => {
    set({ status: 'authenticated', user })
  },
  setAnonymous: () => {
    set({ status: 'anonymous', user: null })
  },
}))
