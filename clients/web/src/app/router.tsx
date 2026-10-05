import { Suspense } from 'react'
import { createBrowserRouter } from 'react-router'
import { HomePage } from './lazy-pages'
import { RouteErrorBoundary, RouteFallback } from './route-boundary'

export const router = createBrowserRouter([
  {
    path: '/',
    errorElement: <RouteErrorBoundary />,
    element: (
      <Suspense fallback={<RouteFallback />}>
        <HomePage />
      </Suspense>
    ),
  },
])
