import { createContext, useContext } from 'react'
import {
  AuthenticationError,
  AuthenticationRateLimitError,
  AuthenticationUnavailableError,
} from './authApi'

export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated' | 'unavailable'

export type AuthContextValue = {
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
  status: AuthStatus
  statusMessage: string | null
  username: string | null
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth() {
  const context = useContext(AuthContext)

  if (context === null) {
    throw new Error('useAuth must be used within AuthProvider.')
  }

  return context
}

export { AuthenticationError, AuthenticationRateLimitError, AuthenticationUnavailableError }
