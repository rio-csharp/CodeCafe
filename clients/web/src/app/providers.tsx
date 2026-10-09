import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEffect, useMemo } from 'react'
import type { ReactNode } from 'react'
import { I18nextProvider, useTranslation } from 'react-i18next'
import { RouterProvider } from 'react-router/dom'
import { bootstrapSession, useSessionStore } from '@/entities/session'
import { FALLBACK_LANGUAGE, i18n } from '@/shared/i18n'
import { router } from './router'

function createQueryClient(_identity?: string): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        refetchOnWindowFocus: false,
        retry: 1,
      },
    },
  })
}

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
 * Resumes a stored session once. Query cache ownership is handled separately
 * by IdentityQueryProvider so identity changes never restart bootstrap.
 */
function SessionBootstrap() {
  useEffect(() => {
    const unsubscribe = bootstrapSession()

    return () => {
      unsubscribe()
    }
  }, [])

  return null
}

/**
 * A cache belongs to exactly one session identity. Replacing the client makes
 * active observers subscribe afresh; cleanup cancels requests and clears every
 * old query so late private responses have nowhere to land.
 */
export function IdentityQueryProvider({ children }: { children: ReactNode }) {
  const identity = useSessionStore((state) =>
    state.status === 'authenticated' ? `user:${state.user?.id ?? 'missing'}` : state.status,
  )
  const client = useMemo(() => createQueryClient(identity), [identity])

  useEffect(
    () => () => {
      void client.cancelQueries()
      client.clear()
    },
    [client],
  )

  return (
    <QueryClientProvider key={identity} client={client}>
      {children}
    </QueryClientProvider>
  )
}

export function Providers() {
  return (
    <I18nextProvider i18n={i18n}>
      <DocumentLanguage />
      <SessionBootstrap />
      <IdentityQueryProvider>
        <RouterProvider router={router} />
      </IdentityQueryProvider>
    </I18nextProvider>
  )
}
