import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Mail } from 'lucide-react'
import AuthLayout from '@/widgets/auth-layout'
import { PasswordInput } from '@/widgets/auth-form'
import GitHubIcon from '@/shared/ui/icons/GitHubIcon'
import { useLoginForm, completePostAuthRedirect } from '@/features/authenticate'

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()

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
      title="Welcome back"
      subtitle="Sign in to continue to your workspace"
      footer={
        <p className="text-sm text-text-secondary">
          Don&apos;t have an account?{' '}
          <Link to={`/register${location.search}`} className="text-brand-brown hover:underline">
            Create an account
          </Link>
        </p>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-5">
        {isError && (
          <p className="text-sm text-status-error text-center">
            {error instanceof Error ? error.message : 'Login failed'}
          </p>
        )}

        <div>
          <label className="block text-sm font-medium text-text-primary mb-2">
            Email
          </label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-text-tertiary" />
            <input
              type="email"
              placeholder="you@example.com"
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
              Password
            </label>
            <span className="text-xs text-text-tertiary">Forgot password? Coming soon</span>
          </div>
          <PasswordInput
            placeholder="Enter your password"
            error={errors.password?.message}
            {...register('password')}
          />
        </div>

        <button
          type="submit"
          disabled={isPending}
          className="w-full rounded-lg bg-text-primary py-2.5 text-sm font-medium text-text-inverse hover:bg-surface-inverse-hover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isPending ? 'Signing in...' : 'Login'}
        </button>
      </form>

      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t border-border-default" />
        </div>
        <div className="relative flex justify-center text-xs">
          <span className="bg-surface px-4 text-text-tertiary">or</span>
        </div>
      </div>

      <button
        type="button"
        disabled
        className="w-full flex items-center justify-center gap-2 rounded-lg border border-border-subtle bg-surface-hover py-2.5 text-sm font-medium text-text-tertiary cursor-not-allowed"
      >
        <GitHubIcon className="h-5 w-5 opacity-50" />
        Continue with GitHub
        <span className="text-xs text-text-tertiary">(Coming soon)</span>
      </button>
    </AuthLayout>
  )
}
