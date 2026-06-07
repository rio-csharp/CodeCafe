import type { TFunction } from 'i18next'
import { ApiError } from '@/shared/api'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import type { MarkdownImportErrorCode } from '@/entities/notebook'

/**
 * Map a Markdown import failure to a translated user-facing message.
 *
 * The server returns ProblemDetails with `{ code, field?, retryable, details? }`
 * extensions and
 * `apiFetch` lifts `code` onto `ApiError.code`. The server's `detail` is a
 * developer-sentence, so we deliberately key off `code` for the user string
 * and fall back to `getErrorMessage` only when the code is unknown.
 */
export function mapImportError(err: unknown, t: TFunction): string {
  if (err instanceof ApiError) {
    const key = codeToTranslationKey(err.code as MarkdownImportErrorCode | undefined)
    if (key) return t(key)
  }
  return getErrorMessage(err, t('notebook.importMarkdownErrorGeneric'))
}

function codeToTranslationKey(code: MarkdownImportErrorCode | undefined): string | null {
  switch (code) {
    case 'invalid_upload_request':
      return 'notebook.importMarkdownErrorInvalidRequest'
    case 'unsupported_upload_media_type':
    case 'invalid_upload_file':
      return 'notebook.importMarkdownErrorNotMarkdown'
    case 'upload_too_large':
      return 'notebook.importMarkdownErrorTooLarge'
    case 'markdown_conversion_failed':
      return 'notebook.importMarkdownErrorConversionFailed'
    case 'invalid_parent':
      return 'notebook.importMarkdownErrorInvalidParent'
    case 'notebook_not_found':
    case 'notebook_item_not_found':
    case 'upload_not_found':
    case 'page_required':
      return 'notebook.importMarkdownErrorNotFound'
    case 'access_denied':
    case 'authentication_required':
      return 'notebook.importMarkdownErrorAccessDenied'
    default:
      return null
  }
}
