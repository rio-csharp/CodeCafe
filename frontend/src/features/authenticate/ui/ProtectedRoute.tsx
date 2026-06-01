import { Navigate } from 'react-router-dom'
import { useMe } from '@/entities/user'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'

interface ProtectedRouteProps {
  children: React.ReactNode
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { data, isPending } = useMe()

  if (isPending) {
    return <RouteGuardSpinner />
  }

  if (!data?.user) {
    return <Navigate to="/login" replace />
  }

  return children
}
