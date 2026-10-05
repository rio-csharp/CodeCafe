export type NotebookVisibility = 'Private' | 'Unlisted' | 'Public'

/** Mirrors the backend `NotebookSort` enum, which travels as a string. */
export type NotebookSort = 'UpdatedDesc' | 'CreatedDesc' | 'TitleAsc'

/** The two orderings the homepage offers. */
export type NotebookSortOption = Extract<NotebookSort, 'UpdatedDesc' | 'TitleAsc'>

export interface NotebookSummary {
  id: string
  title: string
  description: string | null
  slug: string
  visibility: NotebookVisibility
  isFavorite: boolean
  tags: string[]
  pageCount: number
  updatedAtUtc: string
}

export interface NotebookListFilters {
  search: string
  sort: NotebookSortOption
}

/** Page number lives in the infinite query, not in the key. */
export const notebookKeys = {
  all: ['notebooks'],
  list: (filters: NotebookListFilters) => ['notebooks', filters],
}
