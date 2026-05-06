import type { ChatMessage } from '../ai/aiClient'

export type AssistantMessage = {
  id: string
  role: 'assistant' | 'user'
  text: string
}

export type NotesAssistantSession = {
  contextInjected: boolean
  contextNotePath: string | null
  messages: AssistantMessage[]
  modelId: string | null
  previousResponseId: string | null
  providerId: string | null
  requestMessages: ChatMessage[]
}

export type DragState = {
  didMove: boolean
  initialX: number
  initialY: number
  pointerId: number
  startX: number
  startY: number
}

export type PanelPosition = {
  left: number
  top: number
}
