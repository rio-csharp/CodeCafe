import type { BlockDto } from '@/entities/block'

export interface PageShare {
  userId: string
  userName: string
  role: 'Viewer' | 'Editor'
}

export interface PageDetails {
  id: string
  notebookId: string
  title: string
  /** Slug chain built by the server, e.g. `/setup/rust-notes`. */
  path: string
  isArchived: boolean
  isFavorite: boolean
  /** Page-level collaborators; only populated meaningfully for writers. */
  shares: PageShare[]
  /** A flat list — `assembleBlockTree` turns it into the render tree. */
  blocks: BlockDto[]
  createdAtUtc: string
  updatedAtUtc: string
}

export const pageKeys = {
  all: ['pages'],
  byPath: (slug: string, path: string) => ['pages', 'by-path', slug, path],
  revisions: (pageId: string) => ['pages', 'revisions', pageId],
  /** Nested under the page's revisions key so invalidating it covers both logs. */
  blockRevisions: (pageId: string, blockId: string) => ['pages', 'revisions', pageId, 'blocks', blockId],
  trash: (slug: string) => ['pages', 'trash', slug],
  /** The notebook-less key prefix-matches every per-notebook favorites list. */
  favorites: (notebookId?: string) =>
    notebookId === undefined ? ['pages', 'favorites'] : ['pages', 'favorites', notebookId],
  /** Full-text page search; the top-level namespace matches the page slice's scope. */
  search: (query: string) => ['search', query],
}

export type RevisionChangeKind = 'Added' | 'Updated' | 'Deleted' | 'Moved'
export type RevisionSource = 'Human' | 'Ai'

/** One block-level change inside a revision batch. */
export interface BlockRevision {
  blockId: string
  blockVersion: number
  changeKind: RevisionChangeKind
  content: unknown
  source: RevisionSource
  createdAtUtc: string
}

/** One handler transaction in the page's history: a batch of block changes. */
export interface PageRevisionGroup {
  atUtc: string
  source: RevisionSource
  changes: BlockRevision[]
}

export interface CursorPage<T> {
  items: T[]
  nextCursor: string | null
}

/** A reconstructed page state at a past instant; blocks mirror the live BlockDto shape. */
export interface PageRevisionSnapshot {
  atUtc: string
  blocks: BlockDto[]
}
