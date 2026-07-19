import { AlertCircle, RefreshCw } from 'lucide-react'
import { useTranslation } from 'react-i18next'

interface QueryErrorProps {
  /** User-facing message (already localized / sanitized). */
  message?: string
  onRetry?: () => void
  className?: string
}

/** Friendly query-failure state with a retry action. */
export default function QueryError({ message, onRetry, className }: QueryErrorProps) {
  const { t } = useTranslation()
  return (
    <div
      role="alert"
      className={`flex flex-col items-center justify-center gap-3 rounded-xl border border-border-default bg-surface px-6 py-10 text-center ${className ?? ''}`}
    >
      <AlertCircle className="h-6 w-6 text-status-error" />
      <p className="text-sm text-text-secondary">{message ?? t('errors.generic')}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="inline-flex items-center gap-1.5 rounded-lg border border-border-default px-4 py-2 text-sm font-medium text-text-primary hover:bg-surface-hover transition-colors"
        >
          <RefreshCw className="h-3.5 w-3.5" />
          {t('common.tryAgain')}
        </button>
      )}
    </div>
  )
}
