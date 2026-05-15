import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import {
  AuthenticationError,
  AuthenticationRateLimitError,
  AuthenticationUnavailableError,
  useAuth,
} from './AuthProvider'

type RedirectState = {
  from?: {
    hash?: string
    pathname?: string
    search?: string
  }
}

function getRedirectPath(state: RedirectState | null): string {
  if (!state?.from?.pathname) {
    return '/'
  }

  return `${state.from.pathname}${state.from.search ?? ''}${state.from.hash ?? ''}`
}

export function LoginPage() {
  const auth = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const redirectPath = getRedirectPath(location.state as RedirectState | null)

  if (auth.status === 'authenticated') {
    return <Navigate replace to={redirectPath} />
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrorMessage(null)
    setIsSubmitting(true)

    try {
      await auth.login(username.trim(), password)
    } catch (error) {
      if (error instanceof AuthenticationError) {
        setErrorMessage(error.message)
      } else if (error instanceof AuthenticationRateLimitError) {
        setErrorMessage(error.message)
      } else if (error instanceof AuthenticationUnavailableError) {
        setErrorMessage(error.message)
      } else {
        setErrorMessage('Unable to sign in right now. Please try again.')
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section className="auth-screen" aria-label="Login">
      <div className="auth-card">
        <p className="eyebrow">Secure Access</p>
        <h1>Sign in to CodeCafe</h1>
        <p className="auth-copy">
          Registration is disabled. Sign in is only needed for admin-only controls.
        </p>

        {auth.status === 'unavailable' && auth.statusMessage ? (
          <p className="auth-error" role="alert">
            {auth.statusMessage}
          </p>
        ) : null}

        <form className="auth-form" onSubmit={handleSubmit}>
          <label className="auth-field">
            <span>Username</span>
            <input
              autoComplete="username"
              name="username"
              onChange={(event) => setUsername(event.target.value)}
              required
              value={username}
            />
          </label>

          <label className="auth-field">
            <span>Password</span>
            <input
              autoComplete="current-password"
              name="password"
              onChange={(event) => setPassword(event.target.value)}
              required
              type="password"
              value={password}
            />
          </label>

          {errorMessage ? (
            <p className="auth-error" role="alert">
              {errorMessage}
            </p>
          ) : null}

          <button className="primary-button auth-submit" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Signing in…' : 'Sign in'}
          </button>

          <button
            className="settings-secondary-button auth-cancel-button"
            onClick={() => void navigate('/settings')}
            type="button"
          >
            Cancel
          </button>
        </form>

        <p className="auth-hint">
          This page is only for admin access. Browsing the app does not require signing in.
        </p>
      </div>
    </section>
  )
}
