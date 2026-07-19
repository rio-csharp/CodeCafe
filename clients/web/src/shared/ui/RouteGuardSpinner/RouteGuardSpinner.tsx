import { useTranslation } from 'react-i18next'

export default function RouteGuardSpinner() {
  const { t } = useTranslation()
  return (
    <div className="min-h-screen flex items-center justify-center" role="status">
      <div
        className="h-8 w-8 animate-spin rounded-full border-2 border-border-hover border-t-text-primary"
        aria-hidden="true"
      />
      <span className="sr-only">{t('common.loading')}</span>
    </div>
  )
}
