/**
 * API error type. Kept in its own module (no i18n/fetch dependencies) so
 * lightweight consumers (e.g. error message helpers) don't pull in the whole
 * API client chain.
 */
export class ApiError extends Error {
  status: number
  code?: string
  constructor(status: number, message: string, code?: string) {
    super(message)
    this.status = status
    this.code = code
    this.name = 'ApiError'
  }
}
