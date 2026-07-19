import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Mail } from 'lucide-react'
import AuthLayout from '@/widgets/auth-layout'
import { PasswordInput } from '@/widgets/auth-form'
import { Input } from '@/shared/ui/Input'
import GitHubIcon from '@/shared/ui/icons/GitHubIcon'
import { useLoginForm, completePostAuthRedirect } from '@/features/authenticate'
import { getDisplayErrorMessage } from '@/shared/lib/errorUtils'
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
          <Link to={`/register${location.search}`} className="text-brand-brown-text hover:underline">
            {t('auth.createAccount')}
          </Link>
        </p>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-5">
        {isError && (
          <p className="text-sm text-status-error text-center">
            {getDisplayErrorMessage(error, t, t('auth.loginError'))}
          </p>
        )}

        <Input
          type="email"
          label={t('auth.email')}
          placeholder={t('auth.emailPlaceholder')}
          data-testid="login-email"
          iconLeft={<Mail className="h-5 w-5" />}
          error={errors.email?.message}
          {...register('email')}
        />

        <div>
          <div className="flex items-center justify-between mb-2">
            <label htmlFor="login-password" className="block text-sm font-medium text-text-primary">
              {t('auth.password')}
            </label>
          </div>
          <PasswordInput
            id="login-password"
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
