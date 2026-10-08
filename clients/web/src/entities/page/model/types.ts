import type { BlockDto } from '@/entities/block'

export interface PageDetails {
  id: string
  notebookId: string
  title: string
  /** Slug chain built by the server, e.g. `/setup/rust-notes`. */
  path: string
  isArchived: boolean
  isFavorite: boolean
  /** A flat list — `assembleBlockTree` turns it into the render tree. */
  blocks: BlockDto[]
  createdAtUtc: string
  updatedAtUtc: string
}

export const pageKeys = {
  all: ['pages'],
  byPath: (slug: string, path: string) => ['pages', 'by-path', slug, path],
  revisions: (pageId: string) => ['pages', 'revisions', pageId],
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
