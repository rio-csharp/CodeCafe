/**
 * Wire shapes mirrored from `server/src/CodeCafe.Application/Common`.
 * Enums travel as strings; keep these unions in sync with the C# enums.
 */

export type ErrorKind =
  | 'Validation'
  | 'Unauthorized'
  | 'Forbidden'
  | 'NotFound'
  | 'Conflict'
  | 'RateLimited'
  | 'Unexpected'

export interface ApiErrorBody {
  code: string
  message: string
  kind: ErrorKind
}

export interface ResultEnvelope<T> {
  value: T | null
  error: ApiErrorBody | null
  isSuccess: boolean
}

export interface AuthUserDto {
  id: string
  email: string
  displayName: string
}

/** Register, login and refresh all answer with this same session shape. */
export interface AuthSessionDto {
  user: AuthUserDto
  accessToken: string
  /** ISO 8601. */
  accessTokenExpiresAtUtc: string
  refreshToken: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  hasNextPage: boolean
}
