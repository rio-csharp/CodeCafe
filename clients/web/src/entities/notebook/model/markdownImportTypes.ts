/**
 * DTOs for the Markdown upload + import flow.
 * Backend contract: `POST /api/notes/uploads/markdown`,
 * `DELETE /api/notes/uploads/{uploadId}`,
 * `POST /api/notes/notebooks/{slug}/pages/import-markdown`,
 * `PUT /api/notes/notebooks/{slug}/pages/{path}/import-markdown`,
 * `POST /api/notes/notebooks/{slug}/pages/{path}/append-markdown`.
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
  parentPath: string | null
  uploadId: string
  includeContent: boolean
}

export interface MarkdownUpdateRequest {
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

export interface MarkdownImportErrorBody {
  code: string
  detail: string
  title?: string
  status?: number
  field?: string | null
  retryable?: boolean
  details?: Record<string, unknown> | null
}

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
