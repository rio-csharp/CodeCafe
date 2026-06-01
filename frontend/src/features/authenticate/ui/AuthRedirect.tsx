import { Navigate } from 'react-router-dom'
import { useMe } from '@/entities/user'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'

interface AuthRedirectProps {
  children: React.ReactNode
}

export default function AuthRedirect({ children }: AuthRedirectProps) {
  const { data, isPending } = useMe()

  if (isPending) {
    return <RouteGuardSpinner />
  }

  if (data?.user) {
    return <Navigate to="/dashboard" replace />
  }

  return children
}
