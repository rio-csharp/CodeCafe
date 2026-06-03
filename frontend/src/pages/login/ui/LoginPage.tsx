import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Mail } from 'lucide-react'
import AuthLayout from '@/widgets/auth-layout'
import { PasswordInput } from '@/widgets/auth-form'
import GitHubIcon from '@/shared/ui/icons/GitHubIcon'
import { useLoginForm, completePostAuthRedirect } from '@/features/authenticate'
import { useTranslation } from 'react-i18next'

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { t } = useTranslation()

  const handleSuccess = () => {
    completePostAuthRedirect(location.search, navigate)
  }

  const {
    register,
    handleSubmit,
    errors,
    isPending,
    isError,
    error,
  } = useLoginForm(handleSuccess)

  return (
    <AuthLayout
      title={t('auth.loginTitle')}
      subtitle={t('auth.loginSubtitle')}
      footer={
        <p className="text-sm text-text-secondary">
          {t('auth.noAccount')}{' '}
          <Link to={`/register${location.search}`} className="text-brand-brown hover:underline">
            {t('auth.createAccount')}
          </Link>
        </p>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-5">
        {isError && (
          <p className="text-sm text-status-error text-center">
            {error instanceof Error ? error.message : t('auth.loginError')}
          </p>
        )}

        <div>
          <label className="block text-sm font-medium text-text-primary mb-2">
            {t('auth.email')}
          </label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-text-tertiary" />
            <input
              type="email"
              placeholder={t('auth.emailPlaceholder')}
              data-testid="login-email"
              className={`w-full rounded-lg border bg-surface py-2.5 pl-10 pr-4 text-sm text-text-primary outline-none transition-colors focus:border-border-focus ${
                errors.email ? 'border-status-error-border' : 'border-border-default'
              }`}
              {...register('email')}
            />
          </div>
          {errors.email && (
            <p className="mt-1 text-xs text-status-error">{errors.email.message}</p>
          )}
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <label className="block text-sm font-medium text-text-primary">
              {t('auth.password')}
            </label>
            <span className="text-xs text-text-tertiary">{t('auth.forgotPassword')}</span>
          </div>
          <PasswordInput
            placeholder={t('auth.passwordPlaceholder')}
            data-testid="login-password"
            error={errors.password?.message}
            {...register('password')}
          />
        </div>

        <button
          type="submit"
          data-testid="login-submit"
          disabled={isPending}
          className="w-full rounded-lg bg-text-primary py-2.5 text-sm font-medium text-text-inverse hover:bg-surface-inverse-hover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isPending ? t('auth.loginLoading') : t('auth.loginSubmit')}
        </button>
      </form>

      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t border-border-default" />
        </div>
        <div className="relative flex justify-center text-xs">
          <span className="bg-surface px-4 text-text-tertiary">{t('auth.or')}</span>
        </div>
      </div>

      <button
        type="button"
        disabled
        className="w-full flex items-center justify-center gap-2 rounded-lg border border-border-subtle bg-surface-hover py-2.5 text-sm font-medium text-text-tertiary cursor-not-allowed"
      >
        <GitHubIcon className="h-5 w-5 opacity-50" />
        {t('auth.githubLogin')}
        <span className="text-xs text-text-tertiary">{t('auth.githubComingSoon')}</span>
      </button>
    </AuthLayout>
  )
}
