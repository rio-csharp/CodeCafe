/**
 * DTOs for the SPA-only Markdown upload + import flow.
 * Backend contract: `POST /api/notes/uploads/markdown`,
 * `DELETE /api/notes/uploads/{uploadId}`,
 * `POST /api/notes/notebooks/{slug}/pages/import-markdown`.
 *
 * These are feature-local types; the unified import/page endpoint responses
 * are not yet on `entities/notebook-item` because no other feature uses them.
 */

export interface MarkdownUploadResponse {
  uploadId: string
  fileName: string | null
  mediaType: string
  bytesReceived: number
  expiresAtUtc: string
}

export interface MarkdownDiscardResponse {
  uploadId: string
  result: 'discarded' | 'already_absent'
}

export interface MarkdownImportRequest {
  title: string
  /** Path of the parent folder, or null to import at the notebook root. */
  parentPath: string | null
  uploadId: string
  includeContent: boolean
}

export interface MarkdownImportResponse {
  pageId: string
  notebookSlug: string
  title: string
  path: string
  parentId: string | null
  contentFormat: 'tiptap_json' | null
  contentIncluded: boolean
  contentJson: Record<string, unknown> | null
  plainTextContent: string | null
  contentJsonBytes: number
  plainTextLength: number
  tipTapNodeCount: number
  createdAtUtc: string
  updatedAtUtc: string
}

/** Server error body shape (unified across the Markdown import endpoints). */
export interface MarkdownImportErrorBody {
  code: string
  message: string
  field?: string | null
  retryable?: boolean
  details?: Record<string, unknown> | null
}

/** Canonical server `code` values we'll map to translated user messages. */
export type MarkdownImportErrorCode =
  | 'invalid_upload_request'
  | 'invalid_upload_file'
  | 'unsupported_upload_media_type'
  | 'upload_too_large'
  | 'upload_not_found'
  | 'markdown_conversion_failed'
  | 'invalid_parent'
  | 'page_required'
  | 'notebook_not_found'
  | 'notebook_item_not_found'
  | 'access_denied'
  | 'authentication_required'
