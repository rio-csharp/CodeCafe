import { apiDelete, apiJson, apiSend } from '../../lib/apiClient'

export type AiProviderModel = {
  id: string
  modelId: string
  displayName: string
  enabled: boolean
  kind: 'Official' | 'Custom' | string
}

export type AiProvider = {
  id: string
  name: string
  baseUrl: string
  apiKey: string | null
  enabled: boolean
  builtIn: boolean
  models: AiProviderModel[]
}

export type UpsertAiProviderRequest = {
  name: string
  baseUrl: string
  apiKey: string | null
  enabled: boolean
}

export type UpsertAiProviderModelRequest = {
  modelId: string
  displayName: string
  enabled: boolean
  kind: 'Official' | 'Custom'
}

export function listAiProviders() {
  return apiJson<AiProvider[]>('/api/ai/providers')
}

export function createAiProvider(request: UpsertAiProviderRequest) {
  return apiSend<AiProvider>('/api/ai/providers', 'POST', request)
}

export function updateAiProvider(providerId: string, request: UpsertAiProviderRequest) {
  return apiSend<AiProvider>(`/api/ai/providers/${providerId}`, 'PUT', request)
}

export function deleteAiProvider(providerId: string) {
  return apiDelete(`/api/ai/providers/${providerId}`)
}

export function createAiProviderModel(providerId: string, request: UpsertAiProviderModelRequest) {
  return apiSend<AiProviderModel>(`/api/ai/providers/${providerId}/models`, 'POST', request)
}

export function updateAiProviderModel(providerId: string, modelId: string, request: UpsertAiProviderModelRequest) {
  return apiSend<AiProviderModel>(`/api/ai/providers/${providerId}/models/${modelId}`, 'PUT', request)
}

export function deleteAiProviderModel(providerId: string, modelId: string) {
  return apiDelete(`/api/ai/providers/${providerId}/models/${modelId}`)
}
