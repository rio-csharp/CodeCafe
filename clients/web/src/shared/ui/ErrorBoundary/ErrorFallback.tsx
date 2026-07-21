import { useTranslation } from 'react-i18next'
import { AlertTriangle, RefreshCw } from 'lucide-react'

interface ErrorFallbackProps {
  title?: string
  description?: string
  onRetry?: () => void
}

export function ErrorFallback({
  title,
  description,
  onRetry,
}: ErrorFallbackProps) {
  const { t } = useTranslation()
  return (
    <div className="rounded-xl border border-status-error-border bg-status-error-bg p-6">
      <div className="flex items-start gap-3">
        <AlertTriangle className="h-5 w-5 text-status-error shrink-0 mt-0.5" />
        <div>
          <h3 className="text-sm font-semibold text-status-error">{title ?? t('common.errorTitle')}</h3>
          <p className="mt-1 text-xs text-status-error">{description ?? t('common.errorDescription')}</p>
          {onRetry ? (
            <button
              type="button"
              onClick={onRetry}
              className="mt-3 inline-flex items-center gap-1.5 rounded-lg bg-status-error-bg px-3 py-1.5 text-xs font-medium text-status-error hover:bg-status-error-border transition-colors"
            >
              <RefreshCw className="h-3 w-3" />
              {t('common.tryAgain')}
            </button>
          ) : (
            <button
              type="button"
              onClick={() => window.location.reload()}
              className="mt-3 inline-flex items-center gap-1.5 rounded-lg bg-status-error-bg px-3 py-1.5 text-xs font-medium text-status-error hover:bg-status-error-border transition-colors"
            >
              <RefreshCw className="h-3 w-3" />
              {t('common.refreshPage')}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
