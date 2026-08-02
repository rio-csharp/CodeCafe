import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { notesKeys } from '@/entities/notebook'
import { useAiEditStore, useApplyAiEditProposal, useDiscardAiEditProposal } from './useAiEdit'

interface UseAiEditProposalActionsOptions {
  notebookSlug: string | undefined
  notebookId: string
  pagePath: string
  onEnterEditMode: (pagePath: string) => void
}

/**
 * Orchestrates the AI-edit proposal lifecycle for the notebook reader:
 * applying, continuing (hand off to the page editor), discarding, and
 * closing the change preview. Keeps the page component a thin shell (§17).
 */
export function useAiEditProposalActions({
  notebookSlug,
  notebookId,
  pagePath,
  onEnterEditMode,
}: UseAiEditProposalActionsOptions) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const proposal = useAiEditStore((s) => s.proposal)
  const previewOpen = useAiEditStore((s) => s.previewOpen)
  const closePreview = useAiEditStore((s) => s.closePreview)
  const applyProposal = useApplyAiEditProposal(notebookId)
  const discardProposal = useDiscardAiEditProposal()

  const isAiEditPreviewActive =
    previewOpen &&
    proposal !== null &&
    proposal.notebookSlug === notebookSlug &&
    (proposal.operation === 'create_page' || proposal.pagePath === pagePath)

  const handleApplyAiEdit = () => {
    if (!proposal) return
    applyProposal.mutate(
      { applyPath: proposal.applyPath },
      {
        onSuccess: async (result) => {
          await queryClient.refetchQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
          if (result.operation === 'delete_page') {
            navigate(`/notes/${notebookSlug}`)
            return
          }
          if (result.pagePath && result.pagePath !== pagePath) {
            navigate(`/notes/${notebookSlug}/${result.pagePath}`)
          }
        },
      },
    )
  }

  const handleCloseAiEditPreview = () => {
    closePreview()
  }

  const handleDiscardAiEdit = () => {
    if (proposal) discardProposal.mutate({ discardPath: proposal.discardPath })
  }

  const handleContinueAiEdit = () => {
    if (!proposal) return
    if (proposal.operation === 'create_page') {
      applyProposal.mutate(
        { applyPath: proposal.applyPath },
        {
          onSuccess: async (result) => {
            await queryClient.refetchQueries({ queryKey: notesKeys.itemsRoot(notebookId) })
            if (result.pagePath) {
              onEnterEditMode(result.pagePath)
              navigate(`/notes/${notebookSlug}/${result.pagePath}`)
            }
          },
        },
      )
    } else if (proposal.pagePath) {
      closePreview()
      onEnterEditMode(proposal.pagePath)
    }
  }

  return {
    proposal,
    isAiEditPreviewActive,
    isProcessing: applyProposal.isPending || discardProposal.isPending,
    handleApplyAiEdit,
    handleCloseAiEditPreview,
    handleDiscardAiEdit,
    handleContinueAiEdit,
  }
}
