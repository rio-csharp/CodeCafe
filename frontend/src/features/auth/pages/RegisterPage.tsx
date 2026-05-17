import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Mail, User } from 'lucide-react'
import AuthLayout from '../components/AuthLayout'
import PasswordInput from '../components/PasswordInput'
import GitHubIcon from '../../../components/icons/GitHubIcon'
import { useRegister } from '../hooks/useAuth'

const registerSchema = z.object({
  displayName: z
    .string()
    .min(2, '显示名称至少 2 位')
    .max(40, '显示名称最多 40 位'),
  email: z.string().email('请输入有效的邮箱地址'),
  password: z.string().min(8, '密码至少 8 位'),
})

type RegisterFormData = z.infer<typeof registerSchema>

export default function RegisterPage() {
  const [hint, setHint] = useState<string | null>(null)
  const registerMutation = useRegister()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  })

  useEffect(() => {
    if (!hint) return
    const timer = setTimeout(() => setHint(null), 3000)
    return () => clearTimeout(timer)
  }, [hint])

  const onSubmit = (data: RegisterFormData) => {
    registerMutation.mutate(data)
  }

  return (
    <AuthLayout
      title="Create your account"
      subtitle="Start your journey with CodeCafe"
      footer={
        <p className="text-sm text-gray-500">
          Already have an account?{' '}
          <Link to="/login" className="text-brand-brown hover:underline">
            Login
          </Link>
        </p>
      }
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        {registerMutation.isError && (
          <p className="text-sm text-red-500 text-center">
            {registerMutation.error instanceof Error
              ? registerMutation.error.message
              : '注册失败'}
          </p>
        )}

        <div>
          <label className="block text-sm font-medium text-gray-900 mb-2">
            Display name
          </label>
          <div className="relative">
            <User className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
            <input
              type="text"
              placeholder="Enter your name"
              className={`w-full rounded-lg border bg-white py-2.5 pl-10 pr-4 text-sm text-gray-900 outline-none transition-colors focus:border-black ${
                errors.displayName ? 'border-red-300' : 'border-gray-200'
              }`}
              {...register('displayName')}
            />
          </div>
          {errors.displayName && (
            <p className="mt-1 text-xs text-red-500">
              {errors.displayName.message}
            </p>
          )}
        </div>

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
          <label className="block text-sm font-medium text-gray-900 mb-2">
            Password
          </label>
          <PasswordInput
            placeholder="Create a password"
            error={errors.password?.message}
            {...register('password')}
          />
          <p className="mt-1.5 text-xs text-gray-400">
            At least 8 characters, includes letters and numbers
          </p>
        </div>

        {hint && (
          <p className="text-sm text-brand-brown text-center">{hint}</p>
        )}

        <button
          type="submit"
          disabled={registerMutation.isPending}
          className="w-full rounded-lg bg-black py-2.5 text-sm font-medium text-white hover:bg-gray-800 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {registerMutation.isPending ? 'Creating account...' : 'Create account'}
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
        onClick={() => setHint('GitHub login is not supported yet. Coming soon.')}
        className="w-full flex items-center justify-center gap-2 rounded-lg border border-gray-200 bg-white py-2.5 text-sm font-medium text-gray-900 hover:bg-gray-50 transition-colors"
      >
        <GitHubIcon className="h-5 w-5" />
        Continue with GitHub
      </button>
    </AuthLayout>
  )
}
