import { Navigate, useLocation } from 'react-router-dom'
import { useMe } from '@/entities/user'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import { setPostAuthRedirect } from '../lib/postAuthRedirect'

interface ProtectedRouteProps {
  children: React.ReactNode
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { data, isPending } = useMe()
  const location = useLocation()

  if (isPending) {
    return <RouteGuardSpinner />
  }

  if (!data?.user) {
    const returnUrl = `${location.pathname}${location.search}${location.hash}`
    setPostAuthRedirect(returnUrl)
    return <Navigate to={`/login?returnUrl=${encodeURIComponent(returnUrl)}`} replace />
  }

  return children
}
