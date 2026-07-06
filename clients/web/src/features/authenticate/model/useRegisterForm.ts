import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useRegister } from './useRegister'

export type RegisterFormData = z.infer<ReturnType<typeof useRegisterSchema>>

function useRegisterSchema() {
  const { t } = useTranslation()
  return z.object({
    displayName: z
      .string()
      .trim()
      .min(2, t('auth.displayNameMin'))
      .max(40, t('auth.displayNameMax')),
    email: z.string().email(t('auth.emailInvalid')),
    password: z
      .string()
      .min(8, t('auth.passwordMin'))
      .regex(
        /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).*$/,
        t('auth.passwordComplexity')
      ),
  })
}

export function useRegisterForm(onSuccess: () => void) {
  const registerSchema = useRegisterSchema()
  const registerMutation = useRegister()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
  })

  const onSubmit = (data: RegisterFormData) => {
    const trimmed = {
      displayName: data.displayName.trim(),
      email: data.email.trim(),
      password: data.password,
    }
    registerMutation.mutate(trimmed, { onSuccess })
  }

  return {
    register,
    handleSubmit: handleSubmit(onSubmit),
    errors,
    isPending: registerMutation.isPending,
    isError: registerMutation.isError,
    error: registerMutation.error,
  }
}
