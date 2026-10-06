import { z } from 'zod'

export const EMAIL_MAX_LENGTH = 256
export const PASSWORD_MIN_LENGTH = 8
export const PASSWORD_MAX_LENGTH = 128
export const DISPLAY_NAME_MAX_LENGTH = 40

/**
 * Client-side mirrors of the server's FluentValidation rules, so a typo is
 * caught before the round trip. Messages are i18n keys, not copy — `Field`
 * translates them, which keeps every string in `shared/i18n`.
 */
const emailField = z
  .string()
  .trim()
  .email({ error: 'auth.validation.email' })
  .max(EMAIL_MAX_LENGTH, { error: 'auth.validation.emailTooLong' })

export const loginSchema = z.object({
  email: emailField,
  // Login only asks that something was typed; the server owns the real check.
  password: z.string().min(1, { error: 'auth.validation.passwordRequired' }),
})

export const registerSchema = z.object({
  email: emailField,
  password: z
    .string()
    .min(PASSWORD_MIN_LENGTH, { error: 'auth.validation.passwordTooShort' })
    .max(PASSWORD_MAX_LENGTH, { error: 'auth.validation.passwordTooLong' }),
  displayName: z
    .string()
    .trim()
    .min(1, { error: 'auth.validation.displayNameRequired' })
    .max(DISPLAY_NAME_MAX_LENGTH, { error: 'auth.validation.displayNameTooLong' }),
})

export type LoginValues = z.infer<typeof loginSchema>
export type RegisterValues = z.infer<typeof registerSchema>
