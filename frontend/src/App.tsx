import { Routes, Route, useLocation } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import ErrorBoundary from './components/ErrorBoundary'
import HomePage from './features/home/HomePage'
import NotesPage from './features/notes/NotesPage'
import CodesPage from './features/codes/CodesPage'
import AboutPage from './features/about/AboutPage'
import LoginPage from './features/auth/LoginPage'

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

function App() {
  const location = useLocation()

  return (
    <ErrorBoundary>
      <AnimatePresence mode="wait">
        <Routes location={location} key={location.pathname}>
          <Route path="/" element={<PageTransition><HomePage /></PageTransition>} />
          <Route path="/notes" element={<PageTransition><NotesPage /></PageTransition>} />
          <Route path="/codes" element={<PageTransition><CodesPage /></PageTransition>} />
          <Route path="/about" element={<PageTransition><AboutPage /></PageTransition>} />
          <Route path="/login" element={<PageTransition><LoginPage /></PageTransition>} />
        </Routes>
      </AnimatePresence>
    </ErrorBoundary>
  )
}

export default App
