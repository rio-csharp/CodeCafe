import { useMe } from '../auth/hooks/useAuth'

function CodesPage() {
  const { data } = useMe()
  const isLoggedIn = !!data?.user?.id

  return (
    <div className={isLoggedIn ? 'p-8 lg:p-12' : 'pt-32 pb-20'}>
      <div className="mx-auto max-w-7xl px-6 lg:px-8">
        <h1 className="text-4xl font-bold text-black">Codes</h1>
        <p className="mt-4 text-gray-500">Coming soon.</p>
      </div>
    </div>
  )
}

export default CodesPage
