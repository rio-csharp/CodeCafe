import { downloadFile } from '@/shared/api'
import type { DownloadFileOptions } from '@/shared/api'

/** Raw markdown download, not the JSON envelope — the browser saves the file. */
export function exportPage(pageId: string, options?: DownloadFileOptions): Promise<void> {
  return downloadFile(`/api/pages/${encodeURIComponent(pageId)}/export`, options)
}
