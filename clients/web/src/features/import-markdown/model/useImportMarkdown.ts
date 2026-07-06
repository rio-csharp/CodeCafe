import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  discardMarkdownUpload,
  importMarkdownAsPage,
  notesKeys,
  uploadMarkdownFile,
  type MarkdownImportResponse,
} from '@/entities/notebook'

export interface ImportMarkdownInput {
  file: File
  title: string
  parentPath: string | null
}

export type ImportStage = 'uploading' | 'converting' | 'saving'

export interface ImportStageEvent {
  stage: ImportStage
}

export interface UseImportMarkdownOptions {
  /** Called as the mutation enters each step, so the UI can render a status line. */
  onStage?: (event: ImportStageEvent) => void
  /** Called after the page is persisted and queries are invalidated. */
  onSuccess?: (data: MarkdownImportResponse) => void
  /** Called when the upload or import step fails (after best-effort cleanup). */
  onError?: (err: unknown) => void
}

/**
 * Two-step Markdown import orchestrator.
 *
 *  1. Upload the file → receive an `uploadId`.
 *  2. Ask the server to create a page from that upload (Markdown → TipTap
 *     server-side; the upload is auto-consumed on success).
 *
 * If step 2 fails, the orphan upload is best-effort discarded. Cleanup errors
 * are swallowed so they never mask the original import failure.
 *
 * `onStage` fires as the mutation enters each step. `onSuccess` and `onError`
 * are TanStack Query callbacks — they are NOT invoked from a React useEffect,
 * so the orchestrator's caller can perform state updates inside them safely.
 */
export function useImportMarkdown(
  notebookSlug: string,
  notebookId: string,
  options: UseImportMarkdownOptions = {},
) {
  const queryClient = useQueryClient()
  return useMutation<MarkdownImportResponse, unknown, ImportMarkdownInput>({
    mutationFn: async (input) => {
      options.onStage?.({ stage: 'uploading' })
      const upload = await uploadMarkdownFile(input.file)
      try {
        options.onStage?.({ stage: 'converting' })
        // Server returns when the page is persisted (saving is internal to
        // the same call), so fire 'saving' just before so the UI shows it.
        options.onStage?.({ stage: 'saving' })
        return await importMarkdownAsPage(notebookSlug, {
          title: input.title,
          parentPath: input.parentPath,
          uploadId: upload.uploadId,
          includeContent: false,
        })
      } catch (err) {
        // Fire-and-forget cleanup; never let discard errors mask the real one.
        void discardMarkdownUpload(upload.uploadId).catch(() => {})
        throw err
      }
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
      options.onSuccess?.(data)
    },
    onError: (err) => {
      options.onError?.(err)
    },
  })
}
