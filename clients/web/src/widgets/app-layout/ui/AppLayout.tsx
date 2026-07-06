import { useLocation } from 'react-router-dom'
import { useUser } from '@/entities/user'
import Navbar from '@/widgets/navbar'
import Sidebar from '@/widgets/sidebar'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import { LayoutContext } from '@/shared/model/layoutContext'
import { useSidebarStore } from '@/widgets/sidebar'

interface AppLayoutProps {
  children: React.ReactNode
}

export default function AppLayout({ children }: AppLayoutProps) {
  const location = useLocation()
  const { data: authData, isPending } = useUser()
  const isCollapsed = useSidebarStore((s) => s.isCollapsed)

  if (isPending) {
    return <RouteGuardSpinner />
  }

  const user = authData?.user ?? null
  const isAuthenticated = !!user

  // Notebook reader routes render their own full-screen chrome
  const isNotebookRoute = /^\/notes\/[^/]+/.test(location.pathname)
  if (isNotebookRoute) {
    return (
      <LayoutContext.Provider value={{ layout: 'navbar', user }}>
        {children}
      </LayoutContext.Provider>
    )
  }

  if (isAuthenticated) {
    return (
      <LayoutContext.Provider value={{ layout: 'sidebar', user }}>
        <div className="min-h-screen bg-surface flex">
          <Sidebar />
          <main className={`flex-1 transition-all duration-200 ${isCollapsed ? 'md:ml-16' : 'md:ml-[var(--sidebar-width)]'}`}>
            {children}
          </main>
        </div>
      </LayoutContext.Provider>
    )
  }

  return (
    <LayoutContext.Provider value={{ layout: 'navbar', user }}>
      <div className="min-h-screen bg-surface">
        <Navbar />
        {children}
      </div>
    </LayoutContext.Provider>
  )
}
