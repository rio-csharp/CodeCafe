import { CheckCircle, AlertCircle, X } from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { useTranslation } from 'react-i18next'
import { useToastStore } from '@/shared/model/toastStore'

export function ToastContainer() {
  const { t } = useTranslation()
  const { toasts, removeToast } = useToastStore()

  return (
    <div
      role="status"
      aria-live="polite"
      aria-atomic="true"
      className="fixed bottom-6 right-6 z-[100] flex flex-col gap-2"
    >
      <AnimatePresence mode="popLayout">
        {toasts.map((toast) => (
          <motion.div
            key={toast.id}
            layout
            initial={{ opacity: 0, y: 16, scale: 0.96 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, x: 24, scale: 0.96 }}
            transition={{ duration: 0.2, ease: 'easeOut' }}
            className={`flex items-center gap-2 rounded-lg px-4 py-2.5 shadow-lg text-sm font-medium ${
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
              onClick={() => removeToast(toast.id)}
              className="ml-1 p-0.5 hover:bg-white/10 rounded transition-colors"
              aria-label={t('common.dismissNotification')}
            >
              <X className="h-3.5 w-3.5" />
            </button>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  )
}
