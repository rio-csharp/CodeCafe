import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import type { ReactNode } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { ToastContainer } from '@/shared/ui/Toast'
import { ApiError } from '@/shared/api/client'
import '@/shared/lib/i18n'
import { useThemeStore } from '@/shared/model/themeStore'
import { useEffect } from 'react'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      // Low-churn notebook data: avoid refetching on every mount/navigation.
      // Mutations still invalidate explicitly, so freshness is preserved.
      staleTime: 30_000,
      retry: (failureCount, error) => {
        // Do not retry client errors (4xx) — they won't resolve on retry
        if (error instanceof ApiError && error.status >= 400 && error.status < 500) {
          return false
        }
        return failureCount < 3
      },
    },
  },
})

function ThemeInit() {
  const init = useThemeStore((s) => s.init)
  useEffect(() => {
    return init()
  }, [init])
  return null
}

interface AppProvidersProps {
  children: ReactNode
}

export function AppProviders({ children }: AppProvidersProps) {
  return (
    <BrowserRouter>
      <QueryClientProvider client={queryClient}>
        <ThemeInit />
        {children}
        <ToastContainer />
        {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
      </QueryClientProvider>
    </BrowserRouter>
  )
}
