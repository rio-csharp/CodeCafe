import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  appendMarkdownToPage,
  discardMarkdownUpload,
  importMarkdownAsPage,
  notesKeys,
  replacePageContentFromMarkdown,
  uploadMarkdownText,
} from '@/entities/notebook'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { generateAiNoteDraft } from '../api/aiAssistantApi'
import type {
  AiDraftApplyMode,
  AiDraftIntent,
  AiNoteDraftRequest,
  AiNoteDraftResponse,
} from './types'

interface UseGenerateAiNoteDraftOptions {
  draftEndpointPath: string | null
  notebook: Notebook
  activePage: NotebookItem | null
  locale: string
}

interface GenerateDraftInput {
  intent: AiDraftIntent
  prompt: string
}

interface UseApplyAiNoteDraftOptions {
  notebook: Notebook
  activePage: NotebookItem | null
}

interface ApplyDraftInput {
  mode: AiDraftApplyMode
  markdown: string
  title: string
}

export function useGenerateAiNoteDraft({
  draftEndpointPath,
  notebook,
  activePage,
  locale,
}: UseGenerateAiNoteDraftOptions) {
  return useMutation<AiNoteDraftResponse, unknown, GenerateDraftInput>({
    mutationFn: (input) => {
      if (!draftEndpointPath) {
        throw new Error('AI drafts are not configured.')
      }

      const request: AiNoteDraftRequest = {
        notebookSlug: notebook.slug,
        activePagePath: activePage?.path ?? null,
        intent: input.intent,
        prompt: input.prompt,
        locale,
      }

      return generateAiNoteDraft(draftEndpointPath, request)
    },
  })
}

export function useApplyAiNoteDraft({
  notebook,
  activePage,
}: UseApplyAiNoteDraftOptions) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (input: ApplyDraftInput) => {
      if ((input.mode === 'append' || input.mode === 'replace') && !activePage) {
        throw new Error('Choose a page before applying this draft.')
      }

      const upload = await uploadMarkdownText(
        input.markdown,
        `${slugifyFileName(input.title)}.md`,
      )

      try {
        if (input.mode === 'create') {
          return await importMarkdownAsPage(notebook.slug, {
            title: input.title,
            parentPath: getParentPath(activePage?.path ?? null),
            uploadId: upload.uploadId,
            includeContent: false,
          })
        }

        if (input.mode === 'append') {
          return await appendMarkdownToPage(notebook.slug, activePage!.path, {
            uploadId: upload.uploadId,
            includeContent: false,
          })
        }

        return await replacePageContentFromMarkdown(notebook.slug, activePage!.path, {
          uploadId: upload.uploadId,
          includeContent: false,
        })
      } catch (err) {
        void discardMarkdownUpload(upload.uploadId).catch(() => {})
        throw err
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebook.id) })
      queryClient.invalidateQueries({ queryKey: notesKeys.detail(notebook.slug) })
      queryClient.invalidateQueries({ queryKey: notesKeys.all })
    },
  })
}

function getParentPath(pagePath: string | null): string | null {
  if (!pagePath || !pagePath.includes('/')) {
    return null
  }

  return pagePath.split('/').slice(0, -1).join('/') || null
}

function slugifyFileName(title: string): string {
  const slug = title
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')

  return slug || 'ai-draft'
}
