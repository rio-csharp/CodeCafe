export type NotebookVisibility = 'public' | 'private' | 'unlisted'

export interface Notebook {
  id: string
  ownerId: string
  title: string
  slug: string
  description: string
  visibility: NotebookVisibility
  isPublished: boolean
  authorDisplayName: string
  itemCount: number
  folderCount: number
  pageCount: number
  favoriteCount: number
  isFavoritedByMe: boolean
  lastActivityAtUtc: string
  createdAtUtc: string
  updatedAtUtc: string
  canEdit?: boolean
}

export interface NotebookFavorite {
  notebookId: string
  isFavorited: boolean
  favoriteCount: number
}

export interface NotebookItem {
  id: string
  notebookId: string
  parentId: string | null
  type: 'folder' | 'page'
  title: string
  slug: string
  path: string
  sortOrder: number
  contentFormat: 'tiptap_json'
  contentJson: Record<string, unknown> | null
  plainTextContent: string | null
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateNotebookRequest {
  title: string
  description: string
  visibility: NotebookVisibility
}

export interface UpdateNotebookRequest {
  title?: string
  description?: string
  visibility?: NotebookVisibility
  isPublished?: boolean
}

export interface CreateNotebookItemRequest {
  parentId: string | null
  type: 'folder' | 'page'
  title: string
  sortOrder: number
  contentJson?: Record<string, unknown> | null
  plainTextContent?: string | null
}

export interface UpdateNotebookItemRequest {
  title: string
  parentId?: string | null
  sortOrder?: number
  contentJson?: Record<string, unknown> | null
  plainTextContent?: string | null
}

export interface ReorderItemRequest {
  itemId: string
  parentId: string | null
  sortOrder: number
}

export interface ReorderItemsPayload {
  items: ReorderItemRequest[]
}
