import { useMe } from '../features/auth/hooks/useAuth'
import Navbar from '../components/Navbar'
import Sidebar from '../components/Sidebar'
import { LayoutContext } from './LayoutContext'

interface AppLayoutProps {
  children: React.ReactNode
}

export default function AppLayout({ children }: AppLayoutProps) {
  const { data: authData } = useMe()
  const user = authData?.user ?? null
  const isAuthenticated = !!user

  if (isAuthenticated) {
    return (
      <LayoutContext.Provider value={{ layout: 'sidebar', user }}>
        <div className="min-h-screen bg-white flex">
          <Sidebar />
          <main className="flex-1" style={{ marginLeft: 'var(--sidebar-width)' }}>
            {children}
          </main>
        </div>
      </LayoutContext.Provider>
    )
  }

  return (
    <LayoutContext.Provider value={{ layout: 'navbar', user }}>
      <div className="min-h-screen bg-white">
        <Navbar />
        {children}
      </div>
    </LayoutContext.Provider>
  )
}
