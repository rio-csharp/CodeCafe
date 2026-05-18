import ErrorBoundary from './components/ErrorBoundary'
import { AppRouter } from './app/router'
import AppLayout from './app/AppLayout'

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
