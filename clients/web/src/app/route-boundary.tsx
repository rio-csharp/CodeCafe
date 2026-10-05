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
    <div className="flex min-h-dvh flex-col items-center justify-center gap-4 bg-canvas px-4 text-center">
      <h1 className="font-display text-3xl text-ink">{t('routeError.title')}</h1>
      <p className="text-muted">{t('routeError.body')}</p>
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
    <div className="flex min-h-dvh items-center justify-center bg-canvas text-accent">
      <span className="sr-only">{t('catalog.loading')}</span>
      <Spinner className="h-8 w-8" />
    </div>
  )
}
