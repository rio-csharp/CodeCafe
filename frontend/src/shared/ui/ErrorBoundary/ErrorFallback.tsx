import { AlertTriangle, RefreshCw } from 'lucide-react'

interface ErrorFallbackProps {
  title?: string
  description?: string
  onRetry?: () => void
}

export function ErrorFallback({
  title = 'Something went wrong',
  description = 'This part of the UI crashed. You can try refreshing or continue using other features.',
  onRetry,
}: ErrorFallbackProps) {
  return (
    <div className="rounded-xl border border-status-error-border bg-status-error-bg p-6">
      <div className="flex items-start gap-3">
        <AlertTriangle className="h-5 w-5 text-status-error shrink-0 mt-0.5" />
        <div>
          <h3 className="text-sm font-semibold text-status-error">{title}</h3>
          <p className="mt-1 text-xs text-status-error">{description}</p>
          {onRetry ? (
            <button
              type="button"
              onClick={onRetry}
              className="mt-3 inline-flex items-center gap-1.5 rounded-lg bg-status-error-bg px-3 py-1.5 text-xs font-medium text-status-error hover:bg-status-error-border transition-colors"
            >
              <RefreshCw className="h-3 w-3" />
              Try again
            </button>
          ) : (
            <button
              type="button"
              onClick={() => window.location.reload()}
              className="mt-3 inline-flex items-center gap-1.5 rounded-lg bg-status-error-bg px-3 py-1.5 text-xs font-medium text-status-error hover:bg-status-error-border transition-colors"
            >
              <RefreshCw className="h-3 w-3" />
              Refresh page
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
