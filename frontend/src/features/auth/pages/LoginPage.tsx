import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Mail } from 'lucide-react'
import AuthLayout from '../components/AuthLayout'
import PasswordInput from '../components/PasswordInput'
import GitHubIcon from '@/components/icons/GitHubIcon'
import { useLogin } from '../hooks/useAuth'
import { completePostAuthRedirect } from '../lib/postAuthRedirect'

const loginSchema = z.object({
  email: z.string().email('Please enter a valid email address'),
  password: z.string().min(1, 'Please enter your password'),
})

type LoginFormData = z.infer<typeof loginSchema>

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const loginMutation = useLogin()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  })

  const onSubmit = (data: LoginFormData) => {
    loginMutation.mutate(data, {
      onSuccess: () => completePostAuthRedirect(location.search, navigate),
    })
  }

  return (
    <AuthLayout
      title="Welcome back"
      subtitle="Sign in to continue to your workspace"
      footer={
        <p className="text-sm text-gray-500">
          Don&apos;t have an account?{' '}
          <Link to={`/register${location.search}`} className="text-brand-brown hover:underline">
            Create an account
          </Link>
        </p>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        {loginMutation.isError && (
          <p className="text-sm text-red-500 text-center">
            {loginMutation.error instanceof Error
              ? loginMutation.error.message
              : 'Login failed'}
          </p>
        )}

        <div>
          <label className="block text-sm font-medium text-gray-900 mb-2">
            Email
          </label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input
              type="email"
              placeholder="you@example.com"
              className={`w-full rounded-lg border bg-white py-2.5 pl-10 pr-4 text-sm text-gray-900 outline-none transition-colors focus:border-black ${
                errors.email ? 'border-red-300' : 'border-gray-200'
              }`}
              {...register('email')}
            />
          </div>
          {errors.email && (
            <p className="mt-1 text-xs text-red-500">{errors.email.message}</p>
          )}
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <label className="block text-sm font-medium text-gray-900">
              Password
            </label>
            <span className="text-xs text-gray-300">Forgot password? Coming soon</span>
          </div>
          <PasswordInput
            placeholder="Enter your password"
            error={errors.password?.message}
            {...register('password')}
          />
        </div>

        <button
          type="submit"
          disabled={loginMutation.isPending}
          className="w-full rounded-lg bg-black py-2.5 text-sm font-medium text-white hover:bg-gray-800 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loginMutation.isPending ? 'Signing in...' : 'Login'}
        </button>
      </form>

      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <div className="w-full border-t border-gray-200" />
        </div>
        <div className="relative flex justify-center text-xs">
          <span className="bg-white px-4 text-gray-400">or</span>
        </div>
      </div>

      <button
        type="button"
        disabled
        className="w-full flex items-center justify-center gap-2 rounded-lg border border-gray-100 bg-gray-50 py-2.5 text-sm font-medium text-gray-300 cursor-not-allowed"
      >
        <GitHubIcon className="h-5 w-5 opacity-50" />
        Continue with GitHub
        <span className="text-xs text-gray-300">(Coming soon)</span>
      </button>
    </AuthLayout>
  )
}
