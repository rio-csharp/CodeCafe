import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { changePassword, dropSession } from '@/entities/session'
import { ApiError } from '@/shared/api'
import { Field } from '@/features/authenticate'
import { Button, Input, Spinner } from '@/shared/ui'
import { changePasswordSchema } from '../model/schema'
import type { ChangePasswordValues } from '../model/schema'

/**
 * A password change revokes every refresh token server-side, so success means
 * the session is already dead: drop it locally and send the user to login,
 * where a notice explains why.
 */
export function ChangePasswordForm() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [errorKey, setErrorKey] = useState<string | null>(null)

  const {
    register: registerField,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordValues>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setErrorKey(null)
    try {
      await changePassword(values.currentPassword, values.newPassword)
    } catch (error) {
      setErrorKey(
        error instanceof ApiError && error.code === 'incorrect_current_password'
          ? 'account.passwordWrong'
          : 'account.passwordFailed',
      )
      return
    }
    dropSession()
    navigate('/login', { replace: true, state: { notice: 'passwordChanged' } })
  })

  return (
    <form
      noValidate
      className="flex flex-col gap-4"
      onSubmit={(event) => {
        void onSubmit(event)
      }}
    >
      <Field id="current-password" label={t('account.currentPassword')} errorKey={errors.currentPassword?.message}>
        <Input
          id="current-password"
          type="password"
          autoComplete="current-password"
          invalid={errors.currentPassword !== undefined}
          aria-describedby={errors.currentPassword === undefined ? undefined : 'current-password-error'}
          {...registerField('currentPassword')}
        />
      </Field>

      <Field id="new-password" label={t('account.newPassword')} errorKey={errors.newPassword?.message}>
        <Input
          id="new-password"
          type="password"
          autoComplete="new-password"
          invalid={errors.newPassword !== undefined}
          aria-describedby={errors.newPassword === undefined ? undefined : 'new-password-error'}
          {...registerField('newPassword')}
        />
      </Field>

      <Field id="confirm-password" label={t('account.confirmPassword')} errorKey={errors.confirmPassword?.message}>
        <Input
          id="confirm-password"
          type="password"
          autoComplete="new-password"
          invalid={errors.confirmPassword !== undefined}
          aria-describedby={errors.confirmPassword === undefined ? undefined : 'confirm-password-error'}
          {...registerField('confirmPassword')}
        />
      </Field>

      {errorKey === null ? null : (
        <p role="alert" className="rounded-xl border border-line bg-canvas px-4 py-3 text-sm text-accent-strong">
          {t(errorKey)}
        </p>
      )}

      <div>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? <Spinner /> : null}
          {isSubmitting ? t('account.passwordSubmitting') : t('account.passwordSubmit')}
        </Button>
      </div>
    </form>
  )
}
