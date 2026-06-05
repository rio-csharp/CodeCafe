import { apiFetch } from '@/shared/api'
import type {
  MarkdownDiscardResponse,
  MarkdownImportRequest,
  MarkdownImportResponse,
  MarkdownUploadResponse,
} from '../model/types'

/**
 * Upload a Markdown file via multipart/form-data. The browser sets the
 * Content-Type with the boundary; `apiFetch` correctly skips its JSON
 * Content-Type shortcut because the body is not a string.
 */
export async function uploadMarkdownFile(
  file: File,
  fileName?: string,
): Promise<MarkdownUploadResponse> {
  const formData = new FormData()
  formData.append('file', file)
  if (fileName) formData.append('fileName', fileName)
  return apiFetch<MarkdownUploadResponse>('/api/notes/uploads/markdown', {
    method: 'POST',
    body: formData,
  })
}

/**
 * Create a new page in the notebook from a previously uploaded Markdown blob.
 * The server auto-consumes the upload on success; the response carries the
 * persisted page metadata.
 */
export async function importMarkdownAsPage(
  notebookSlug: string,
  request: MarkdownImportRequest,
): Promise<MarkdownImportResponse> {
  return apiFetch<MarkdownImportResponse>(
    `/api/notes/notebooks/${encodeURIComponent(notebookSlug)}/pages/import-markdown`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}

/**
 * Best-effort cleanup when the second step of the import flow fails after
 * the upload succeeded. The endpoint is idempotent; failures are swallowed
 * at the call site.
 */
export async function discardMarkdownUpload(uploadId: string): Promise<MarkdownDiscardResponse> {
  return apiFetch<MarkdownDiscardResponse>(
    `/api/notes/uploads/${encodeURIComponent(uploadId)}`,
    { method: 'DELETE' },
  )
}
