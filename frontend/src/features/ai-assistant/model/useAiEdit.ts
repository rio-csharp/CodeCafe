import { create } from 'zustand'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { notesKeys } from '@/entities/notebook'
import { useToast } from '@/shared/ui/Toast'
import { getErrorMessage } from '@/shared/lib/errorUtils'
import {
  applyAiEditProposal,
  createAiEditProposal,
  discardAiEditProposal,
} from '../api/aiAssistantApi'
import type { AiEditOperation, AiEditResponse } from './types'

interface AiEditState {
  proposal: AiEditResponse | null
  previewOpen: boolean
  setProposal: (proposal: AiEditResponse | null) => void
  clearProposal: () => void
  openPreview: () => void
  closePreview: () => void
}

export const useAiEditStore = create<AiEditState>((set) => ({
  proposal: null,
  previewOpen: false,
  setProposal: (proposal) => set({ proposal, previewOpen: proposal !== null }),
  clearProposal: () => set({ proposal: null, previewOpen: false }),
  openPreview: () => set({ previewOpen: true }),
  closePreview: () => set({ previewOpen: false }),
}))

interface UseCreateAiEditProposalOptions {
  editEndpointPath: string | null
  notebook: Notebook
  activePage: NotebookItem | null
}

export function useCreateAiEditProposal({
  editEndpointPath,
  notebook,
  activePage,
}: UseCreateAiEditProposalOptions) {
  const { t, i18n } = useTranslation()

  return useMutation<
    AiEditResponse,
    unknown,
    { prompt: string; operation?: AiEditOperation; apply?: boolean }
  >({
    mutationFn: ({ prompt, operation = 'auto', apply = false }) => {
      if (!editEndpointPath) {
        throw new Error(t('ai.edit.errors.notConfigured'))
      }
      return createAiEditProposal(editEndpointPath, {
        notebookSlug: notebook.slug,
        activePagePath: activePage?.path ?? null,
        prompt,
        operation,
        locale: i18n.resolvedLanguage ?? i18n.language,
        apply,
      })
    },
  })
}

export function useApplyAiEditProposal(notebookId: string) {
  const { t } = useTranslation()
  const { showToast } = useToast()
  const queryClient = useQueryClient()
  const clearProposal = useAiEditStore((s) => s.clearProposal)

  return useMutation<AiEditResponse, unknown, { applyPath: string }>({
    mutationFn: ({ applyPath }) => applyAiEditProposal(applyPath),
    onSuccess: () => {
      clearProposal()
      queryClient.invalidateQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
      showToast(t('ai.edit.applied'))
    },
    onError: (err) => {
      showToast(getErrorMessage(err, t('ai.edit.errors.applyFailed')), 'error')
    },
  })
}

export function useDiscardAiEditProposal() {
  const { t } = useTranslation()
  const { showToast } = useToast()
  const clearProposal = useAiEditStore((s) => s.clearProposal)

  return useMutation<void, unknown, { discardPath: string }>({
    mutationFn: ({ discardPath }) => discardAiEditProposal(discardPath),
    onSuccess: () => {
      clearProposal()
      showToast(t('ai.edit.discarded'))
    },
    onError: (err) => {
      showToast(getErrorMessage(err, t('ai.edit.errors.discardFailed')), 'error')
    },
  })
}
