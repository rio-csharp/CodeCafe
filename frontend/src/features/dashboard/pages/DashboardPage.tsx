import Navbar from '../../../components/Navbar'
import { useMe } from '../../auth/hooks/useAuth'

export default function DashboardPage() {
  const { data } = useMe()

  return (
    <div className="min-h-screen bg-white">
      <Navbar />
      <main className="pt-32 pb-20 px-6 lg:px-8 max-w-7xl mx-auto">
        <h1 className="text-4xl font-bold text-black">Dashboard</h1>
        <p className="mt-4 text-gray-500">
          Welcome{data?.user?.displayName ? `, ${data.user.displayName}` : ''}.
        </p>
      </main>
    </div>
  )
}
