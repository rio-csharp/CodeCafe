import { Component, type ErrorInfo, type ReactNode } from 'react'
import { ErrorFallback } from './ErrorFallback'

interface Props {
  children: ReactNode
  fallback?: ReactNode
  /** Invoked when a descendant throws. Use this to forward to Sentry/Datadog/etc. */
  onError?: (error: Error, errorInfo: ErrorInfo) => void
  /**
   * When this value changes while the boundary is showing its fallback, the
   * boundary resets and retries rendering its children. Pass e.g. the current
   * location pathname at the app root so a transient error does not lock the
   * whole page until a manual reload.
   */
  resetKey?: string
}

interface State {
  hasError: boolean
  error?: Error
}

class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props)
    this.state = { hasError: false }
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    // Log to the browser console as a baseline signal; the optional onError
    // prop lets host apps route this to their telemetry/observability stack.
    console.error('[ErrorBoundary] Uncaught error:', error, errorInfo)
    this.props.onError?.(error, errorInfo)
  }

  componentDidUpdate(prevProps: Props): void {
    if (this.state.hasError && prevProps.resetKey !== this.props.resetKey) {
      this.setState({ hasError: false, error: undefined })
    }
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback !== undefined) {
        return this.props.fallback
      }
      return <ErrorFallback />
    }
    return this.props.children
  }
}

export default ErrorBoundary
