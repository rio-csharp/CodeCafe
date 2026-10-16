/**
 * Client-side guard for the import dialogs; the server still enforces its own
 * 5 MB request limit, so this one errs on the strict side to fail early.
 */
export const MARKDOWN_FILE_MAX_BYTES = 4 * 1024 * 1024

export const MARKDOWN_FILE_ACCEPT = '.md,.markdown,.txt'

export type MarkdownFileRead =
  | { ok: true; fileName: string; markdown: string }
  | { ok: false; reason: 'tooLarge' | 'unreadable' }

export async function readMarkdownFile(file: File): Promise<MarkdownFileRead> {
  if (file.size > MARKDOWN_FILE_MAX_BYTES) {
    return { ok: false, reason: 'tooLarge' }
  }

  try {
    return { ok: true, fileName: file.name, markdown: await file.text() }
  } catch {
    return { ok: false, reason: 'unreadable' }
  }
}
