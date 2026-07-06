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
  editEndpointPath: string | null
  draftEndpointPath: string | null
}

export type AiAssistantRunState = 'idle' | 'running' | 'error'

export type AiEditOperation =
  | 'auto'
  | 'replace_current_page'
  | 'append_to_current_page'
  | 'create_page'
  | 'delete_page'

export type AiEditMode = 'full_document' | 'operations'

export interface AiEditRequest {
  notebookSlug: string
  activePagePath: string | null
  prompt: string
  operation: AiEditOperation
  locale: string
  apply: boolean
  parentPath?: string | null
  expectedUpdatedAtUtc?: string | null
}

export interface AiToolActivity {
  id: string
  name: string
  status: 'running' | 'done'
  args?: string
  result?: string
}

export type AiAssistantVisibleMessage = Extract<Message, { role: 'assistant' | 'user' }>

export interface AiEditResponse {
  proposalId: string
  previewPath: string
  applyPath: string
  discardPath: string
  expiresAtUtc: string
  operation: AiEditOperation
  mode: AiEditMode
  applied: boolean
  summary: string
  notebookId: string
  notebookSlug: string
  notebookTitle: string
  title: string
  pageId: string | null
  pagePath: string | null
  parentPath: string | null
  beforeContentJson: Record<string, unknown> | null
  beforePlainTextContent: string | null
  afterContentJson: Record<string, unknown>
  afterPlainTextContent: string | null
  operationsJson: Record<string, unknown> | null
  afterContentJsonBytes: number
  afterPlainTextLength: number
  afterTipTapNodeCount: number
  generatedAtUtc: string
  savedAtUtc: string | null
}
