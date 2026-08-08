export interface NotebookItem {
  id: string
  notebookId: string
  parentId: string | null
  type: 'folder' | 'page'
  title: string
  slug: string
  path: string
  sortOrder: number
  contentFormat: 'tiptap_json' | null
  contentJson: Record<string, unknown> | null
  plainTextContent: string | null
  isArchived: boolean
  archivedAtUtc: string | null
  archivedByUserId: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface CreateNotebookItemRequest {
  parentId: string | null
  type: 'folder' | 'page'
  title: string
  sortOrder: number
  contentJson?: Record<string, unknown> | null
}

export interface UpdateNotebookItemRequest {
  title: string
  parentId?: string | null
  sortOrder?: number
  contentJson?: Record<string, unknown> | null
  /**
   * Optimistic-concurrency token: the `updatedAtUtc` the client based its
   * edit on. The backend answers 409 when the stored row is newer. Optional
   * so older backends that don't know the field simply ignore it.
   */
  expectedUpdatedAtUtc?: string | null
}

export interface ReorderItemRequest {
  itemId: string
  parentId: string | null
  sortOrder: number
}

export interface ReorderItemsPayload {
  items: ReorderItemRequest[]
}
