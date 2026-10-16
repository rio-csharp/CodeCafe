import { z } from 'zod'
import { PASSWORD_MAX_LENGTH, PASSWORD_MIN_LENGTH } from '@/features/authenticate'

/**
 * Mirrors ChangePasswordCommandValidator (same policy as registration), plus
 * the client-only confirm field. Messages are i18n keys, translated by Field.
 */
export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, { error: 'auth.validation.passwordRequired' }),
    newPassword: z
      .string()
      .min(PASSWORD_MIN_LENGTH, { error: 'auth.validation.passwordTooShort' })
      .max(PASSWORD_MAX_LENGTH, { error: 'auth.validation.passwordTooLong' }),
    confirmPassword: z.string(),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    error: 'account.passwordMismatch',
    path: ['confirmPassword'],
  })

export type ChangePasswordValues = z.infer<typeof changePasswordSchema>
