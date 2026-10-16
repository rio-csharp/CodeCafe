import { createBrowserRouter } from 'react-router'
import { AccountPage, HomePage, LoginPage, NotebookReaderPage, RegisterPage, SearchPage, TrashPage } from './lazy-pages'
import { LazyPage, RouteErrorBoundary } from './route-boundary'

export const router = createBrowserRouter([
  {
    path: '/',
    errorElement: <RouteErrorBoundary />,
    element: (
      <LazyPage>
        <HomePage />
      </LazyPage>
    ),
  },
  {
    path: '/login',
    errorElement: <RouteErrorBoundary />,
    element: (
      <LazyPage>
        <LoginPage />
      </LazyPage>
    ),
  },
  {
    path: '/register',
    errorElement: <RouteErrorBoundary />,
    element: (
      <LazyPage>
        <RegisterPage />
      </LazyPage>
    ),
  },
  {
    path: '/search',
    errorElement: <RouteErrorBoundary />,
    element: (
      <LazyPage>
        <SearchPage />
      </LazyPage>
    ),
  },
  {
    path: '/account',
    errorElement: <RouteErrorBoundary />,
    element: (
      <LazyPage>
        <AccountPage />
      </LazyPage>
    ),
  },
  {
    path: '/trash',
    errorElement: <RouteErrorBoundary />,
    element: (
      <LazyPage>
        <TrashPage />
      </LazyPage>
    ),
  },
  {
    // The splat is the page path; it matches the bare notebook root too.
    path: '/notebooks/:slug/*',
    errorElement: <RouteErrorBoundary />,
    element: (
      <LazyPage>
        <NotebookReaderPage />
      </LazyPage>
    ),
  },
])
