import { createContext, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import {
  AuthenticationError,
  AuthenticationRateLimitError,
  AuthenticationUnavailableError,
  type AuthSession,
  getSession,
  login as loginRequest,
  logout as logoutRequest,
} from './authApi'

export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated' | 'unavailable'

type AuthContextValue = {
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => Promise<void>
  status: AuthStatus
  statusMessage: string | null
  username: string | null
}

const AuthContext = createContext<AuthContextValue | null>(null)

function toStatus(session: AuthSession): AuthStatus {
  return session.isAuthenticated ? 'authenticated' : 'unauthenticated'
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession>({ isAuthenticated: false, username: null })
  const [status, setStatus] = useState<AuthStatus>('loading')
  const [statusMessage, setStatusMessage] = useState<string | null>(null)

  useEffect(() => {
    let active = true

    void (async () => {
      try {
        const nextSession = await getSession()

        if (!active) {
          return
        }

        setSession(nextSession)
        setStatus(nextSession.isAuthenticated ? 'authenticated' : 'unauthenticated')
        setStatusMessage(null)
      } catch {
        if (!active) {
          return
        }

        setStatus('unavailable')
        setStatusMessage('Authentication service is unavailable right now.')
      }
    })()

    return () => {
      active = false
    }
  }, [])

  async function login(username: string, password: string) {
    const nextSession = await loginRequest(username, password)
    setSession(nextSession)
    setStatus(toStatus(nextSession))
    setStatusMessage(null)
  }

  async function logout() {
    try {
      await logoutRequest()
      setSession({ isAuthenticated: false, username: null })
      setStatus('unauthenticated')
      setStatusMessage(null)
    } catch (error) {
      if (error instanceof AuthenticationUnavailableError) {
        setStatusMessage(error.message)
      }

      throw error
    }
  }

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated: status === 'authenticated',
        login,
        logout,
        status,
        statusMessage,
        username: session.username,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)

  if (context === null) {
    throw new Error('useAuth must be used within AuthProvider.')
  }

  return context
}

export { AuthenticationError }
export { AuthenticationRateLimitError }
export { AuthenticationUnavailableError }
