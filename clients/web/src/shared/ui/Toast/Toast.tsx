import { CheckCircle, AlertCircle, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useToastStore } from '@/shared/model/toastStore'

export function ToastContainer() {
  const { t } = useTranslation()
  const { toasts, leavingIds, dismissToast } = useToastStore()

  return (
    <div
      role="status"
      aria-live="polite"
      aria-atomic="true"
      className="fixed bottom-6 right-6 z-[100] flex flex-col gap-2"
    >
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={`flex items-center gap-2 rounded-lg px-4 py-2.5 shadow-lg text-sm font-medium ${
            leavingIds.includes(toast.id) ? 'toast-leaving' : 'toast-enter'
          } ${
            toast.type === 'success'
              ? 'bg-text-primary text-text-inverse'
              : 'bg-status-error text-text-inverse'
          }`}
        >
          {toast.type === 'success' ? (
            <CheckCircle className="h-4 w-4 shrink-0" />
          ) : (
            <AlertCircle className="h-4 w-4 shrink-0" />
          )}
          <span>{toast.message}</span>
          <button
            type="button"
            onClick={() => dismissToast(toast.id)}
            className="ml-1 p-0.5 hover:bg-white/10 rounded transition-colors"
            aria-label={t('common.dismissNotification')}
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      ))}
    </div>
  )
}
