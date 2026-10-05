import { useTranslation } from 'react-i18next'
import { useRouteError } from 'react-router'
import { Button, Spinner } from '@/shared/ui'

export function RouteErrorBoundary() {
  const { t } = useTranslation()
  const error = useRouteError()

  if (import.meta.env.DEV) {
    console.error('Route render failed', error)
  }

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-4 bg-cream px-4 text-center">
      <h1 className="font-display text-3xl text-roast">{t('routeError.title')}</h1>
      <p className="text-mocha">{t('routeError.body')}</p>
      <Button
        onClick={() => {
          window.location.reload()
        }}
      >
        {t('routeError.reload')}
      </Button>
    </div>
  )
}

export function RouteFallback() {
  const { t } = useTranslation()

  return (
    <div className="flex min-h-dvh items-center justify-center bg-cream text-caramel">
      <span className="sr-only">{t('catalog.loading')}</span>
      <Spinner className="h-8 w-8" />
    </div>
  )
}
