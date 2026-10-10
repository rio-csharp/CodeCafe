import { z } from 'zod'
import { DISPLAY_NAME_MAX_LENGTH } from '@/features/authenticate'

/**
 * Mirrors UpdateProfileCommandValidator: required, trimmed, at most the
 * server's display-name limit. Messages are i18n keys, translated by Field.
 */
export const profileSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(1, { error: 'auth.validation.displayNameRequired' })
    .max(DISPLAY_NAME_MAX_LENGTH, { error: 'auth.validation.displayNameTooLong' }),
})

export type ProfileValues = z.infer<typeof profileSchema>
