import { Navigate, useLocation } from 'react-router-dom'
import { useUser } from '@/entities/user'
import RouteGuardSpinner from '@/shared/ui/RouteGuardSpinner'
import { getPostAuthRedirect, resolvePostAuthRedirect } from '../lib/postAuthRedirect'

interface AuthRedirectProps {
  children: React.ReactNode
}

export default function AuthRedirect({ children }: AuthRedirectProps) {
  const { data, isPending } = useUser()
  const location = useLocation()

  if (isPending) {
    return <RouteGuardSpinner />
  }

  if (data?.user) {
    const target = resolvePostAuthRedirect(location.search) ?? getPostAuthRedirect() ?? '/dashboard'
    if (target.startsWith('/')) {
      return <Navigate to={target} replace />
    }

    window.location.assign(target)
    return <RouteGuardSpinner />
  }

  return children
}
