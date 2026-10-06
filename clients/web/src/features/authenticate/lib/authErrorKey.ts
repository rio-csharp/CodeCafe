import { ApiError } from '@/shared/api'

/** Server error codes the two forms translate; everything else is generic. */
const ERROR_KEY_BY_CODE: Readonly<Record<string, string>> = {
  invalid_credentials: 'auth.error.invalidCredentials',
  email_already_registered: 'auth.error.emailTaken',
}

export const GENERIC_AUTH_ERROR_KEY = 'auth.error.generic'

export function authErrorKey(error: unknown): string {
  if (error instanceof ApiError) {
    return ERROR_KEY_BY_CODE[error.code] ?? GENERIC_AUTH_ERROR_KEY
  }
  return GENERIC_AUTH_ERROR_KEY
}
