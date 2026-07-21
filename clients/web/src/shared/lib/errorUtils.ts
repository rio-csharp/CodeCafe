import type { TFunction } from 'i18next'
import { ApiError } from '@/shared/api/ApiError'

/**
 * Extract a human-readable message from an unknown error value.
 * Use this in mutation onError handlers instead of repeating
 * `err instanceof Error ? err.message : fallback` everywhere.
 */
export function getErrorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback
}

/**
 * User-facing variant of getErrorMessage for rendering in the UI.
 * - Known backend error codes map to localized `errors.<code>` messages.
 * - Other 4xx messages are shown as-is (they are authored for users,
 *   e.g. "slug already taken").
 * - 5xx ProblemDetails `detail` is never shown — it may contain internal
 *   paths/field names. The localized fallback is used instead.
 */
export function getDisplayErrorMessage(err: unknown, t: TFunction, fallback: string): string {
  if (err instanceof ApiError) {
    if (err.code) {
      const localized = t(`errors.${err.code}`, { defaultValue: '' })
      if (localized) return localized
    }
    if (err.status >= 400 && err.status < 500) {
      return err.message
    }
    return fallback
  }
  return err instanceof Error ? err.message : fallback
}
