import type { ErrorKind } from './types'

const STATUS_TO_KIND: Readonly<Record<number, ErrorKind>> = {
  400: 'Validation',
  401: 'Unauthorized',
  403: 'Forbidden',
  404: 'NotFound',
  409: 'Conflict',
  429: 'RateLimited',
}

/** Everything above `shared/` sees when a request fails — never the envelope itself. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly kind: ErrorKind

  constructor(args: {
    status: number
    code: string
    kind: ErrorKind
    message: string
    cause?: unknown
  }) {
    super(args.message, args.cause === undefined ? undefined : { cause: args.cause })
    this.name = 'ApiError'
    this.status = args.status
    this.code = args.code
    this.kind = args.kind
  }
}

export function kindFromStatus(status: number): ErrorKind {
  return STATUS_TO_KIND[status] ?? 'Unexpected'
}
