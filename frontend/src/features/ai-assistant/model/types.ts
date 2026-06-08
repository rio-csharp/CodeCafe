import type { Message } from '@ag-ui/core'
import type { Notebook } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'

export interface AiAssistantNotebookContext {
  notebook: Notebook
  activePage: NotebookItem | null
}

export interface AiStatus {
  enabled: boolean
  endpointPath: string | null
  draftEndpointPath: string | null
}

export type AiAssistantRunState = 'idle' | 'running' | 'error'

export type AiDraftIntent =
  | 'summarize'
  | 'outline'
  | 'rewrite'
  | 'expand'
  | 'continue'
  | 'custom'

export type AiDraftApplyMode = 'create' | 'append' | 'replace'

export interface AiToolActivity {
  id: string
  name: string
  status: 'running' | 'done'
  args?: string
  result?: string
}

export type AiAssistantVisibleMessage = Extract<Message, { role: 'assistant' | 'user' }>

export interface AiNoteDraftRequest {
  notebookSlug: string
  activePagePath: string | null
  intent: AiDraftIntent
  prompt: string
  locale: string
}

export interface AiNoteDraftResponse {
  markdown: string
  title: string
  intent: AiDraftIntent
  notebookSlug: string
  pagePath: string | null
  generatedAtUtc: string
}
