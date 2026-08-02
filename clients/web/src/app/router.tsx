import { Suspense, lazy } from 'react'
import { Routes, Route, useLocation, Navigate } from 'react-router-dom'
import PageTransition from '@/shared/ui/PageTransition'
import { ProtectedRoute, AuthRedirect } from '@/features/authenticate'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'

const HomePage = lazy(() => import('@/pages/home'))
const NotesSelectionPage = lazy(() => import('@/pages/notes'))
const NotebookReaderPage = lazy(() => import('@/pages/notebook-reader'))
const CreateNotebookPage = lazy(() => import('@/pages/notebook-create'))
const EditNotebookPage = lazy(() => import('@/pages/notebook-edit'))
const CodesPage = lazy(() => import('@/pages/codes'))
const AboutPage = lazy(() => import('@/pages/about'))
const LoginPage = lazy(() => import('@/pages/login'))
const RegisterPage = lazy(() => import('@/pages/register'))
const DashboardPage = lazy(() => import('@/pages/dashboard'))

function LazyWrapper({ children }: { children: React.ReactNode }) {
  return (
    <ErrorBoundary>
      <Suspense fallback={<RouteGuardSpinner />}>
        {children}
      </Suspense>
    </ErrorBoundary>
  )
}

function useRouteKey() {
  const location = useLocation()
  const pathname = location.pathname

  // For notebook reader sub-routes, use the notebook slug as key
  // to prevent full page animation when switching between pages
  const notebookMatch = pathname.match(/^\/notes\/([^/]+)/)
  if (notebookMatch && pathname !== '/notes') {
    return `/notes/${notebookMatch[1]}`
  }

  return pathname
}

export function AppRouter() {
  const location = useLocation()
  const routeKey = useRouteKey()

  return (
    <Routes location={location} key={routeKey}>
        <Route
          path="/"
          element={
            <PageTransition>
              <LazyWrapper><HomePage /></LazyWrapper>
            </PageTransition>
          }
        />
        <Route
          path="/notes"
          element={
            <PageTransition>
              <LazyWrapper><NotesSelectionPage /></LazyWrapper>
            </PageTransition>
          }
        />
        <Route
          path="/notes/new"
          element={
            <PageTransition>
              <ProtectedRoute>
                <LazyWrapper><CreateNotebookPage /></LazyWrapper>
              </ProtectedRoute>
            </PageTransition>
          }
        />
        <Route
          path="/notes/:notebookSlug/edit"
          element={
            <PageTransition>
              <ProtectedRoute>
                <LazyWrapper><EditNotebookPage /></LazyWrapper>
              </ProtectedRoute>
            </PageTransition>
          }
        />
        <Route
          path="/notes/:notebookSlug/*"
          element={
            <PageTransition>
              <LazyWrapper><NotebookReaderPage /></LazyWrapper>
            </PageTransition>
          }
        />
        <Route
          path="/codes"
          element={
            <PageTransition>
              <LazyWrapper><CodesPage /></LazyWrapper>
            </PageTransition>
          }
        />
        <Route
          path="/about"
          element={
            <PageTransition>
              <LazyWrapper><AboutPage /></LazyWrapper>
            </PageTransition>
          }
        />
        <Route
          path="/login"
          element={
            <PageTransition>
              <AuthRedirect>
                <LazyWrapper><LoginPage /></LazyWrapper>
              </AuthRedirect>
            </PageTransition>
          }
        />
        <Route
          path="/register"
          element={
            <PageTransition>
              <AuthRedirect>
                <LazyWrapper><RegisterPage /></LazyWrapper>
              </AuthRedirect>
            </PageTransition>
          }
        />
        <Route
          path="/dashboard"
          element={
            <PageTransition>
              <ProtectedRoute>
                <LazyWrapper><DashboardPage /></LazyWrapper>
              </ProtectedRoute>
            </PageTransition>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
  )
}
