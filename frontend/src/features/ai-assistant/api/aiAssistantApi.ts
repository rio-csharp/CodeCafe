import { apiFetch } from '@/shared/api/client'
import { AI_STATUS_ENDPOINT_PATH } from '@/shared/config'
import type { AiNoteDraftRequest, AiNoteDraftResponse, AiStatus } from '../model/types'

export async function getAiStatus(): Promise<AiStatus> {
  return apiFetch<AiStatus>(AI_STATUS_ENDPOINT_PATH)
}

export async function generateAiNoteDraft(
  endpointPath: string,
  request: AiNoteDraftRequest,
): Promise<AiNoteDraftResponse> {
  return apiFetch<AiNoteDraftResponse>(endpointPath, {
    method: 'POST',
    body: JSON.stringify(request),
  })
}
