import { downloadFile } from '@/shared/api'
import type { DownloadFileOptions } from '@/shared/api'

/** Raw markdown download, not the JSON envelope — the browser saves the file. */
export function exportNotebook(idOrSlug: string, options?: DownloadFileOptions): Promise<void> {
  return downloadFile(`/api/notebooks/${encodeURIComponent(idOrSlug)}/export`, options)
}
