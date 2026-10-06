export type NotebookVisibility = 'Private' | 'Unlisted' | 'Public'

/** Mirrors the backend `NotebookSort` enum, which travels as a string. */
export type NotebookSort = 'UpdatedDesc' | 'CreatedDesc' | 'TitleAsc'

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
  /** Attribution for the shelf: whose notebook this is. */
  ownerDisplayName: string

}

export interface NotebookListFilters {
  search: string
  sort: NotebookSort
}

/** The authenticated list can additionally filter by ownership-side flags. */
export interface MyNotebookListFilters extends NotebookListFilters {
  favoritesOnly: boolean
  visibility: NotebookVisibility | null
}

/**
 * Full notebook metadata. `isOwner` / `canWrite` are the caller's relationship
 * to it — both false for anonymous readers, which is all M4 does with them.
 */
export interface NotebookDetails {
  id: string
  title: string
  description: string | null
  slug: string
  visibility: NotebookVisibility
  hasAccessCode: boolean
  tags: string[]
  pageCount: number
  createdAtUtc: string
  updatedAtUtc: string
  isOwner: boolean
  canWrite: boolean
}

/** One node of the notebook's page hierarchy. */
export interface PageTreeNode {
  id: string
  title: string
  /** Slug chain built by the server, e.g. `/setup/rust-notes`. */
  path: string
  sortOrder: number
  isArchived: boolean
  isFavorite: boolean
  children: PageTreeNode[]
}

export interface NotebookTree {
  notebookId: string
  roots: PageTreeNode[]
}

/**
 * Page number lives in the infinite query, not in the key. Every namespace here
 * is distinct so logging in cannot hand one cache another's entries.
 */
export const notebookKeys = {
  all: ['notebooks'],
  publicList: (filters: NotebookListFilters) => ['notebooks', 'public', filters],
  mine: () => ['notebooks', 'mine'],
  myList: (filters: MyNotebookListFilters) => ['notebooks', 'mine', filters],
  myFavorites: () => ['notebooks', 'mine', 'favorites'],
  trash: () => ['notebooks', 'trash'],
  details: (slug: string) => ['notebooks', 'details', slug],
  tree: (slug: string) => ['notebooks', 'tree', slug],
}
