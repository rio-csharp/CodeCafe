import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEffect } from 'react'
import { I18nextProvider, useTranslation } from 'react-i18next'
import { RouterProvider } from 'react-router/dom'
import { FALLBACK_LANGUAGE, i18n } from '@/shared/i18n'
import { router } from './router'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
})

/** Keeps `<html lang>` in step with the active language. */
function DocumentLanguage() {
  const { i18n: instance } = useTranslation()

  useEffect(() => {
    const sync = () => {
      document.documentElement.lang = instance.resolvedLanguage ?? FALLBACK_LANGUAGE
    }

    sync()
    instance.on('languageChanged', sync)
    return () => {
      instance.off('languageChanged', sync)
    }
  }, [instance])

  return null
}

export function Providers() {
  return (
    <QueryClientProvider client={queryClient}>
      <I18nextProvider i18n={i18n}>
        <DocumentLanguage />
        <RouterProvider router={router} />
      </I18nextProvider>
    </QueryClientProvider>
  )
}
