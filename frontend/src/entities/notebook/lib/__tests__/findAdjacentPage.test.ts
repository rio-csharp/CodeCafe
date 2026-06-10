import { describe, expect, it } from 'vitest'
import { buildTree } from '../buildTree'
import { findAdjacentPage } from '../findAdjacentPage'
import type { NotebookItem } from '@/entities/notebook-item'

function makeItem(overrides: Partial<NotebookItem>): NotebookItem {
  return {
    id: overrides.id ?? 'id',
    notebookId: 'nb',
    parentId: overrides.parentId ?? null,
    type: overrides.type ?? 'page',
    title: overrides.title ?? 'Title',
    slug: overrides.slug ?? 'slug',
    path: overrides.path ?? 'slug',
    sortOrder: overrides.sortOrder ?? 0,
    contentFormat: null,
    contentJson: null,
    plainTextContent: null,
    isArchived: false,
    archivedAtUtc: null,
    archivedByUserId: null,
    createdAtUtc: '2024-01-01T00:00:00Z',
    updatedAtUtc: null,
    ...overrides,
  }
}

describe('findAdjacentPage', () => {
  it('returns prev and next for a middle page at root level', () => {
    const items = [
      makeItem({ id: 'p1', type: 'page', sortOrder: 0, path: 'p1' }),
      makeItem({ id: 'p2', type: 'page', sortOrder: 1, path: 'p2' }),
      makeItem({ id: 'p3', type: 'page', sortOrder: 2, path: 'p3' }),
    ]
    const tree = buildTree(items)
    const result = findAdjacentPage(tree, 'p2')
    expect(result.prev?.id).toBe('p1')
    expect(result.next?.id).toBe('p3')
  })

  it('returns null prev for the first page', () => {
    const items = [
      makeItem({ id: 'p1', type: 'page', sortOrder: 0, path: 'p1' }),
      makeItem({ id: 'p2', type: 'page', sortOrder: 1, path: 'p2' }),
    ]
    const tree = buildTree(items)
    const result = findAdjacentPage(tree, 'p1')
    expect(result.prev).toBeNull()
    expect(result.next?.id).toBe('p2')
  })

  it('returns null next for the last page', () => {
    const items = [
      makeItem({ id: 'p1', type: 'page', sortOrder: 0, path: 'p1' }),
      makeItem({ id: 'p2', type: 'page', sortOrder: 1, path: 'p2' }),
    ]
    const tree = buildTree(items)
    const result = findAdjacentPage(tree, 'p2')
    expect(result.prev?.id).toBe('p1')
    expect(result.next).toBeNull()
  })

  it('skips folders and returns adjacent pages only', () => {
    const items = [
      makeItem({ id: 'f1', type: 'folder', sortOrder: 0, path: 'f1' }),
      makeItem({ id: 'p1', type: 'page', parentId: 'f1', sortOrder: 1, path: 'f1/p1' }),
      makeItem({ id: 'p2', type: 'page', parentId: 'f1', sortOrder: 2, path: 'f1/p2' }),
      makeItem({ id: 'f2', type: 'folder', sortOrder: 3, path: 'f2' }),
      makeItem({ id: 'p3', type: 'page', parentId: 'f2', sortOrder: 4, path: 'f2/p3' }),
    ]
    const tree = buildTree(items)
    const result = findAdjacentPage(tree, 'p2')
    expect(result.prev?.id).toBe('p1')
    expect(result.next?.id).toBe('p3')
  })

  it('returns null for both when there is only one page', () => {
    const items = [
      makeItem({ id: 'p1', type: 'page', sortOrder: 0, path: 'p1' }),
    ]
    const tree = buildTree(items)
    const result = findAdjacentPage(tree, 'p1')
    expect(result.prev).toBeNull()
    expect(result.next).toBeNull()
  })

  it('returns null for both when page id is not found', () => {
    const items = [
      makeItem({ id: 'p1', type: 'page', sortOrder: 0, path: 'p1' }),
    ]
    const tree = buildTree(items)
    const result = findAdjacentPage(tree, 'nonexistent')
    expect(result.prev).toBeNull()
    expect(result.next).toBeNull()
  })
})
