/**
 * Extract a human-readable message from an unknown error value.
 * Use this in mutation onError handlers instead of repeating
 * `err instanceof Error ? err.message : fallback` everywhere.
 */
export function getErrorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback
}
