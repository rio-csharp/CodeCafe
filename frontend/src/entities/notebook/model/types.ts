export type NotebookVisibility = 'public' | 'private' | 'unlisted'

export interface Notebook {
  id: string
  ownerId: string
  title: string
  slug: string
  description: string | null
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
  updatedAtUtc: string | null
  publishedAtUtc: string | null
  canEdit?: boolean
}

export interface NotebookFavorite {
  notebookId: string
  isFavorited: boolean
  favoriteCount: number
}

export interface CreateNotebookRequest {
  title: string
  description: string
  visibility: NotebookVisibility
}

export interface UpdateNotebookRequest {
  title?: string
  description?: string | null
  visibility?: NotebookVisibility
  isPublished?: boolean
}
