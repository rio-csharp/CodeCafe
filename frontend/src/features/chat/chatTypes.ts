export type ChatPreferences = {
  maxOutputTokens: number | null
  systemPrompt: string
  temperature: number | null
  topP: number | null
}

export type SessionMessage = {
  id: string
  role: 'assistant' | 'user'
  text: string
}

export type ChatSession = {
  id: string
  modelId: string | null
  providerId: string | null
  title: string
  updatedAt: string
  messages: SessionMessage[]
}
