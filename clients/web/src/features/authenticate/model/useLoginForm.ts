import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useLogin } from './useLogin'

export type LoginFormData = z.infer<ReturnType<typeof useLoginSchema>>

function useLoginSchema() {
  const { t } = useTranslation()
  return z.object({
    email: z.string().email(t('auth.emailInvalid')),
    password: z.string().min(1, t('auth.passwordRequired')),
  })
}

export function useLoginForm(onSuccess: () => void) {
  const loginSchema = useLoginSchema()
  const loginMutation = useLogin()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  })

  const onSubmit = (data: LoginFormData) => {
    loginMutation.mutate(data, { onSuccess })
  }

  return {
    register,
    handleSubmit: handleSubmit(onSubmit),
    errors,
    isPending: loginMutation.isPending,
    isError: loginMutation.isError,
    error: loginMutation.error,
  }
}
