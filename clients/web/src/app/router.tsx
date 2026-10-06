import { createBrowserRouter } from 'react-router'
import { HomePage, LoginPage, RegisterPage } from './lazy-pages'
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
])
