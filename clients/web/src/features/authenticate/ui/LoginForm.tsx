import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { login, useSessionStore } from '@/entities/session'
import { Button, Input, Spinner } from '@/shared/ui'
import { authErrorKey } from '../lib/authErrorKey'
import { redirectTo } from '../lib/redirectTo'
import { loginSchema } from '../model/schema'
import type { LoginValues } from '../model/schema'
import { Field } from './Field'

export function LoginForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()
  const setAuthenticated = useSessionStore((state) => state.setAuthenticated)
  const [errorKey, setErrorKey] = useState<string | null>(null)

  const {
    register: registerField,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setErrorKey(null)
    try {
      const session = await login(values)
      setAuthenticated(session.user)
      navigate(redirectTo(location.state), { replace: true })
    } catch (error) {
      setErrorKey(authErrorKey(error))
    }
  })

  return (
    <form
      noValidate
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        void onSubmit(event)
      }}
    >
      <Field id="login-email" label={t('auth.email')} errorKey={errors.email?.message}>
        <Input
          id="login-email"
          type="email"
          autoComplete="email"
          placeholder="you@example.com"
          invalid={errors.email !== undefined}
          aria-describedby={errors.email === undefined ? undefined : 'login-email-error'}
          {...registerField('email')}
        />
      </Field>

      <Field id="login-password" label={t('auth.password')} errorKey={errors.password?.message}>
        <Input
          id="login-password"
          type="password"
          autoComplete="current-password"
          invalid={errors.password !== undefined}
          aria-describedby={errors.password === undefined ? undefined : 'login-password-error'}
          {...registerField('password')}
        />
      </Field>

      {errorKey === null ? null : (
        <p role="alert" className="rounded-xl border border-line bg-canvas px-4 py-3 text-sm text-accent-strong">
          {t(errorKey)}
        </p>
      )}

      <Button type="submit" disabled={isSubmitting} className="mt-2 w-full">
        {isSubmitting ? <Spinner /> : null}
        {t('auth.submitLogin')}
      </Button>
    </form>
  )
}
