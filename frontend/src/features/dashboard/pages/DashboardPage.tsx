import { Link } from 'react-router-dom'
import { FileText, Code } from 'lucide-react'
import Navbar from '../../../components/Navbar'
import { useMe } from '../../auth/hooks/useAuth'

export default function DashboardPage() {
  const { data } = useMe()

  const cards = [
    { to: '/notes', label: 'Notes', icon: FileText, desc: 'Manage your notes' },
    { to: '/codes', label: 'Codes', icon: Code, desc: 'Browse your code snippets' },
  ]

  return (
    <div className="min-h-screen bg-white">
      <Navbar />
      <main className="pt-32 pb-20 px-6 lg:px-8 max-w-7xl mx-auto">
        <h1 className="text-4xl font-bold text-black">Dashboard</h1>
        <p className="mt-4 text-gray-500">
          Welcome{data?.user?.displayName ? `, ${data.user.displayName}` : ''}.
        </p>

        <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {cards.map(({ to, label, icon: Icon, desc }) => (
            <Link
              key={to}
              to={to}
              className="group flex flex-col rounded-2xl border border-gray-200 bg-white p-6 transition-all hover:border-black hover:shadow-sm"
            >
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-gray-100 transition-colors group-hover:bg-black">
                <Icon className="h-6 w-6 text-gray-700 transition-colors group-hover:text-white" />
              </div>
              <h2 className="mt-4 text-lg font-semibold text-black">{label}</h2>
              <p className="mt-1 text-sm text-gray-500">{desc}</p>
            </Link>
          ))}
        </div>
      </main>
    </div>
  )
}
