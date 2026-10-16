import { useTranslation } from 'react-i18next'
import { Link, Navigate, useLocation } from 'react-router'
import { useSessionStore } from '@/entities/session'
import { RegisterForm } from '@/features/authenticate'
import { AuthLayout } from '@/widgets/auth-layout'

export function RegisterPage() {
  const { t } = useTranslation()
  const location = useLocation()
  const status = useSessionStore((state) => state.status)

  if (status === 'authenticated') {
    return <Navigate to="/" replace />
  }

  return (
    <AuthLayout
      title={t('auth.registerTitle')}
      footer={
        <Link to="/login" state={location.state} className="text-accent hover:text-accent-strong">
          {t('auth.toLogin')}
        </Link>
      }
    >
      <RegisterForm />
    </AuthLayout>
  )
}
