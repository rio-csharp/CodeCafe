import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useRegister } from './useRegister'

const registerSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(2, 'Display name must be at least 2 characters')
    .max(40, 'Display name must be at most 40 characters'),
  email: z.string().email('Please enter a valid email address'),
  password: z
    .string()
    .min(8, 'Password must be at least 8 characters')
    .regex(
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).*$/,
      'Password must include uppercase, lowercase, number, and special character'
    ),
})

export type RegisterFormData = z.infer<typeof registerSchema>

export function useRegisterForm(onSuccess: () => void) {
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
