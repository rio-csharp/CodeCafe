import { Routes, Route, useLocation, Navigate } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useMe } from '../features/auth/hooks/useAuth'
import RouteGuardSpinner from '../components/RouteGuardSpinner'
import HomePage from '../features/home/HomePage'
import NotesSelectionPage from '../features/notes/pages/NotesSelectionPage'
import NotebookReaderPage from '../features/notes/pages/NotebookReaderPage'
import CreateNotebookPage from '../features/notes/pages/CreateNotebookPage'
import EditNotebookPage from '../features/notes/pages/EditNotebookPage'
import CodesPage from '../features/codes/CodesPage'
import AboutPage from '../features/about/AboutPage'
import LoginPage from '../features/auth/pages/LoginPage'
import RegisterPage from '../features/auth/pages/RegisterPage'
import DashboardPage from '../features/dashboard/pages/DashboardPage'

function PageTransition({ children }: { children: React.ReactNode }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -12 }}
      transition={{ duration: 0.25, ease: 'easeOut' }}
    >
      {children}
    </motion.div>
  )
}

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { data, isPending } = useMe()

  if (isPending) {
    return <RouteGuardSpinner />
  }

  if (!data?.user) {
    return <Navigate to="/login" replace />
  }

  return children
}

function AuthRedirect({ children }: { children: React.ReactNode }) {
  const { data, isPending } = useMe()

  if (isPending) {
    return <RouteGuardSpinner />
  }

  if (data?.user) {
    return <Navigate to="/dashboard" replace />
  }

  return children
}

export function AppRouter() {
  const location = useLocation()

  return (
    <AnimatePresence mode="wait">
      <Routes location={location} key={location.pathname}>
        <Route
          path="/"
          element={
            <PageTransition>
              <HomePage />
            </PageTransition>
          }
        />
        <Route
          path="/notes"
          element={
            <PageTransition>
              <NotesSelectionPage />
            </PageTransition>
          }
        />
        <Route
          path="/notes/new"
          element={
            <PageTransition>
              <ProtectedRoute>
                <CreateNotebookPage />
              </ProtectedRoute>
            </PageTransition>
          }
        />
        <Route
          path="/notes/:notebookSlug/edit"
          element={
            <PageTransition>
              <ProtectedRoute>
                <EditNotebookPage />
              </ProtectedRoute>
            </PageTransition>
          }
        />
        <Route
          path="/notes/:notebookSlug/*"
          element={
            <PageTransition>
              <NotebookReaderPage />
            </PageTransition>
          }
        />
        <Route
          path="/codes"
          element={
            <PageTransition>
              <CodesPage />
            </PageTransition>
          }
        />
        <Route
          path="/about"
          element={
            <PageTransition>
              <AboutPage />
            </PageTransition>
          }
        />
        <Route
          path="/login"
          element={
            <PageTransition>
              <AuthRedirect>
                <LoginPage />
              </AuthRedirect>
            </PageTransition>
          }
        />
        <Route
          path="/register"
          element={
            <PageTransition>
              <AuthRedirect>
                <RegisterPage />
              </AuthRedirect>
            </PageTransition>
          }
        />
        <Route
          path="/dashboard"
          element={
            <PageTransition>
              <ProtectedRoute>
                <DashboardPage />
              </ProtectedRoute>
            </PageTransition>
          }
        />
      </Routes>
    </AnimatePresence>
  )
}
