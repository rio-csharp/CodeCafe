import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEffect } from 'react'
import { I18nextProvider, useTranslation } from 'react-i18next'
import type { DataRouter } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { bootstrapSession, useSessionStore } from '@/entities/session'
import { onSessionCleared } from '@/shared/api'
import { FALLBACK_LANGUAGE, i18n } from '@/shared/i18n'
import { RouteFallback } from './route-boundary'
import { router as appRouter } from './router'

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

/**
 * Resumes a stored session, and keeps the cache honest whenever the identity behind it changes:
 * a signed-in user's payloads must not survive sign-out (a shared browser must not hand A's
 * notebooks to B), and an anonymous visitor's answers must not survive sign-in — a private
 * notebook answers that visitor with 404, so the owner would inherit "doesn't exist".
 */
function SessionBootstrap() {
  useEffect(() => {
    const unsubscribe = bootstrapSession()
    const unsubscribeCleared = onSessionCleared(() => {
      queryClient.clear()
    })
    const unsubscribeIdentity = useSessionStore.subscribe((state, previous) => {
      if (state.user?.id === previous.user?.id) {
        return
      }
      if (state.user === null) {
        queryClient.clear()
        return
      }
      void queryClient.invalidateQueries()
    })

    return () => {
      unsubscribe()
      unsubscribeCleared()
      unsubscribeIdentity()
    }
  }, [])

  return null
}

/**
 * Routes mount only once the session is known. The access token lives in memory, so every hard
 * load starts without one while the refresh token is still being exchanged; a route that queried
 * in that window would ask anonymously, and a private notebook answers an anonymous caller with
 * 404 — the reader would latch onto "this notebook doesn't exist" although the owner is signed in.
 */
function SessionGatedRouter({ router }: { router: DataRouter }) {
  const status = useSessionStore((state) => state.status)

  return status === 'unknown' ? <RouteFallback /> : <RouterProvider router={router} />
}

export interface ProvidersProps {
  /** The app router; tests start on their own URL by passing a memory router. */
  router?: DataRouter
}

export function Providers({ router = appRouter }: ProvidersProps = {}) {
  return (
    <QueryClientProvider client={queryClient}>
      <I18nextProvider i18n={i18n}>
        <DocumentLanguage />
        <SessionBootstrap />
        <SessionGatedRouter router={router} />
      </I18nextProvider>
    </QueryClientProvider>
  )
}
