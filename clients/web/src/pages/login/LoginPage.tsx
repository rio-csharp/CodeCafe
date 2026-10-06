import { useTranslation } from 'react-i18next'
import { Link, Navigate, useLocation } from 'react-router'
import { useSessionStore } from '@/entities/session'
import { LoginForm } from '@/features/authenticate'
import { AuthLayout } from '@/widgets/auth-layout'

export function LoginPage() {
  const { t } = useTranslation()
  const location = useLocation()
  const status = useSessionStore((state) => state.status)

  // Already signed in: the form has nothing to offer.
  if (status === 'authenticated') {
    return <Navigate to="/" replace />
  }

  return (
    <AuthLayout
      title={t('auth.loginTitle')}
      footer={
        <Link
          to="/register"
          state={location.state}
          className="text-accent hover:text-accent-strong"
        >
          {t('auth.toRegister')}
        </Link>
      }
    >
      <LoginForm />
    </AuthLayout>
  )
}
