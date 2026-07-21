import ErrorBoundary from '@/shared/ui/ErrorBoundary'
import { AppRouter } from './router'
import AppLayout from '@/widgets/app-layout'

function App() {
  return (
    <ErrorBoundary>
      <AppLayout>
        <AppRouter />
      </AppLayout>
    </ErrorBoundary>
  )
}

export default App
