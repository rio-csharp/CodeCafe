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
}
