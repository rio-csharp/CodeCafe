import { useMe } from '../features/auth/hooks/useAuth'
import Navbar from '../components/Navbar'
import Sidebar from '../components/Sidebar'
import RouteGuardSpinner from '../components/RouteGuardSpinner'

interface AppLayoutProps {
  children: React.ReactNode
}

export default function AppLayout({ children }: AppLayoutProps) {
  const { data: authData, isPending } = useMe()

  if (isPending) {
    return <RouteGuardSpinner />
  }

  const isAuthenticated = !!authData?.user?.id

  if (isAuthenticated) {
    return (
      <div className="min-h-screen bg-white flex">
        <Sidebar />
        <main className="flex-1 ml-60">{children}</main>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-white">
      <Navbar />
      {children}
    </div>
  )
}
