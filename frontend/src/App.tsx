import ErrorBoundary from './components/ErrorBoundary'
import { AppRouter } from './app/router'

function App() {
  return (
    <ErrorBoundary>
      <AppRouter />
    </ErrorBoundary>
  )
}

export default App
