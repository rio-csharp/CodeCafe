import { apiFetch } from '@/shared/api/client'
import { AI_STATUS_ENDPOINT_PATH } from '@/shared/config'
import type { AiEditRequest, AiEditResponse, AiStatus } from '../model/types'

export async function getAiStatus(signal?: AbortSignal): Promise<AiStatus> {
  return apiFetch<AiStatus>(AI_STATUS_ENDPOINT_PATH, { signal })
}

export async function createAiEditProposal(
  endpointPath: string,
  request: AiEditRequest,
): Promise<AiEditResponse> {
  return apiFetch<AiEditResponse>(endpointPath, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export async function applyAiEditProposal(applyPath: string): Promise<AiEditResponse> {
  return apiFetch<AiEditResponse>(applyPath, {
    method: 'POST',
  })
}

export async function discardAiEditProposal(discardPath: string): Promise<void> {
  return apiFetch<void>(discardPath, {
    method: 'DELETE',
  })
}
