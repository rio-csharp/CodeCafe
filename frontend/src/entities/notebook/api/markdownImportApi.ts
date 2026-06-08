import { apiFetch } from '@/shared/api'
import type {
  MarkdownDiscardResponse,
  MarkdownImportRequest,
  MarkdownImportResponse,
  MarkdownUpdateRequest,
  MarkdownUploadResponse,
} from '../model/markdownImportTypes'

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

export async function uploadMarkdownText(
  markdown: string,
  fileName = 'ai-draft.md',
): Promise<MarkdownUploadResponse> {
  const file = new File([markdown], fileName, { type: 'text/markdown' })
  return uploadMarkdownFile(file, fileName)
}

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

export async function replacePageContentFromMarkdown(
  notebookSlug: string,
  pagePath: string,
  request: MarkdownUpdateRequest,
): Promise<MarkdownImportResponse> {
  return apiFetch<MarkdownImportResponse>(
    `/api/notes/notebooks/${encodeURIComponent(notebookSlug)}/pages/${encodePagePath(pagePath)}/import-markdown`,
    {
      method: 'PUT',
      body: JSON.stringify(request),
    },
  )
}

export async function appendMarkdownToPage(
  notebookSlug: string,
  pagePath: string,
  request: MarkdownUpdateRequest,
): Promise<MarkdownImportResponse> {
  return apiFetch<MarkdownImportResponse>(
    `/api/notes/notebooks/${encodeURIComponent(notebookSlug)}/pages/${encodePagePath(pagePath)}/append-markdown`,
    {
      method: 'POST',
      body: JSON.stringify(request),
    },
  )
}

export async function discardMarkdownUpload(uploadId: string): Promise<MarkdownDiscardResponse> {
  return apiFetch<MarkdownDiscardResponse>(
    `/api/notes/uploads/${encodeURIComponent(uploadId)}`,
    { method: 'DELETE' },
  )
}

function encodePagePath(path: string): string {
  return path
    .split('/')
    .filter(Boolean)
    .map(encodeURIComponent)
    .join('/')
}
