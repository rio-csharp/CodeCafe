import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { updateProfile, useSessionStore } from '@/entities/session'
import { Field } from '@/features/authenticate'
import { Button, Input, Spinner } from '@/shared/ui'
import { profileSchema } from '../model/schema'
import type { ProfileValues } from '../model/schema'

/**
 * The display-name card. On success the returned user replaces the one in the
 * session store, so the header updates without a reload.
 */
export function ProfileForm() {
  const { t } = useTranslation()
  const user = useSessionStore((state) => state.user)
  const setAuthenticated = useSessionStore((state) => state.setAuthenticated)
  const [saved, setSaved] = useState(false)
  const [failed, setFailed] = useState(false)

  const {
    register: registerField,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ProfileValues>({
    resolver: zodResolver(profileSchema),
    defaultValues: { displayName: user?.displayName ?? '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setSaved(false)
    setFailed(false)
    try {
      const updated = await updateProfile(values.displayName)
      setAuthenticated(updated)
      setSaved(true)
    } catch {
      setFailed(true)
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
      <Field id="profile-display-name" label={t('auth.displayName')} errorKey={errors.displayName?.message}>
        <Input
          id="profile-display-name"
          type="text"
          autoComplete="nickname"
          invalid={errors.displayName !== undefined}
          aria-describedby={errors.displayName === undefined ? undefined : 'profile-display-name-error'}
          {...registerField('displayName')}
        />
      </Field>

      {saved ? (
        <p role="status" className="rounded-xl border border-line bg-canvas px-4 py-3 text-sm text-success">
          {t('account.profileSaved')}
        </p>
      ) : null}
      {failed ? (
        <p role="alert" className="rounded-xl border border-line bg-canvas px-4 py-3 text-sm text-accent-strong">
          {t('account.profileFailed')}
        </p>
      ) : null}

      <div>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? <Spinner /> : null}
          {isSubmitting ? t('account.profileSaving') : t('account.profileSave')}
        </Button>
      </div>
    </form>
  )
}
