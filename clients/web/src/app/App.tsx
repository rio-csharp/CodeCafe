import { useLocation } from 'react-router-dom'
import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { AppRouter } from './router'
import AppLayout from '@/widgets/app-layout'

function App() {
  const location = useLocation()
  return (
    // resetKey lets the top-level boundary recover from a transient error on
    // navigation instead of locking the whole page until a manual reload.
    <ErrorBoundary resetKey={location.pathname}>
      <AppLayout>
        <AppRouter />
      </AppLayout>
    </ErrorBoundary>
  )
}

export default App
