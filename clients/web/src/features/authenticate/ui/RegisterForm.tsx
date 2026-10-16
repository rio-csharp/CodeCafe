import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useLocation, useNavigate } from 'react-router'
import { register as registerAccount, useSessionStore } from '@/entities/session'
import { Button, Input, Spinner } from '@/shared/ui'
import { authErrorKey } from '../lib/authErrorKey'
import { redirectTo } from '../lib/redirectTo'
import { registerSchema } from '../model/schema'
import type { RegisterValues } from '../model/schema'
import { Field } from './Field'

export function RegisterForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const location = useLocation()
  const setAuthenticated = useSessionStore((state) => state.setAuthenticated)
  const [errorKey, setErrorKey] = useState<string | null>(null)

  const {
    register: registerField,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: '', password: '', displayName: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setErrorKey(null)
    try {
      const session = await registerAccount(values)
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
      <Field id="register-name" label={t('auth.displayName')} errorKey={errors.displayName?.message}>
        <Input
          id="register-name"
          type="text"
          autoComplete="nickname"
          invalid={errors.displayName !== undefined}
          aria-describedby={errors.displayName === undefined ? undefined : 'register-name-error'}
          {...registerField('displayName')}
        />
      </Field>

      <Field id="register-email" label={t('auth.email')} errorKey={errors.email?.message}>
        <Input
          id="register-email"
          type="email"
          autoComplete="email"
          placeholder="you@example.com"
          invalid={errors.email !== undefined}
          aria-describedby={errors.email === undefined ? undefined : 'register-email-error'}
          {...registerField('email')}
        />
      </Field>

      <Field id="register-password" label={t('auth.password')} errorKey={errors.password?.message}>
        <Input
          id="register-password"
          type="password"
          autoComplete="new-password"
          invalid={errors.password !== undefined}
          aria-describedby={errors.password === undefined ? undefined : 'register-password-error'}
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
        {t('auth.submitRegister')}
      </Button>
    </form>
  )
}
