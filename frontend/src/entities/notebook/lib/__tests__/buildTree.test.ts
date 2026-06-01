import { describe, it, expect } from 'vitest'
import { buildTree, findFirstPage, findPageByPath, flattenTree } from '../buildTree'
import type { NotebookItem } from '@/entities/notebook-item'

function makeItem(overrides: Partial<NotebookItem>): NotebookItem {
  return {
    id: 'id',
    notebookId: 'nb',
    parentId: null,
    type: 'page',
    title: 'Item',
    slug: 'item',
    path: 'item',
    sortOrder: 0,
    contentFormat: 'tiptap_json',
    contentJson: null,
    plainTextContent: null,
    isArchived: false,
    archivedAtUtc: null,
    archivedByUserId: null,
    createdAtUtc: '',
    updatedAtUtc: '',
    ...overrides,
  }
}

describe('buildTree', () => {
  it('returns empty array for empty input', () => {
    expect(buildTree([])).toEqual([])
  })

  it('builds roots from flat items', () => {
    const items = [
      makeItem({ id: 'a', parentId: null, sortOrder: 0 }),
      makeItem({ id: 'b', parentId: null, sortOrder: 1 }),
    ]
    const tree = buildTree(items)
    expect(tree).toHaveLength(2)
    expect(tree[0].item.id).toBe('a')
    expect(tree[1].item.id).toBe('b')
  })

  it('nests children under parents', () => {
    const items = [
      makeItem({ id: 'folder', parentId: null, type: 'folder', sortOrder: 0 }),
      makeItem({ id: 'page1', parentId: 'folder', type: 'page', sortOrder: 0 }),
      makeItem({ id: 'page2', parentId: 'folder', type: 'page', sortOrder: 1 }),
    ]
    const tree = buildTree(items)
    expect(tree).toHaveLength(1)
    expect(tree[0].children).toHaveLength(2)
    expect(tree[0].children[0].item.id).toBe('page1')
    expect(tree[0].children[1].item.id).toBe('page2')
  })

  it('handles deep nesting', () => {
    const items = [
      makeItem({ id: 'f1', parentId: null, type: 'folder', sortOrder: 0 }),
      makeItem({ id: 'f2', parentId: 'f1', type: 'folder', sortOrder: 0 }),
      makeItem({ id: 'p1', parentId: 'f2', type: 'page', sortOrder: 0 }),
    ]
    const tree = buildTree(items)
    expect(tree[0].children[0].children[0].item.id).toBe('p1')
  })

  it('sorts by sortOrder', () => {
    const items = [
      makeItem({ id: 'z', parentId: null, sortOrder: 10 }),
      makeItem({ id: 'a', parentId: null, sortOrder: 1 }),
    ]
    const tree = buildTree(items)
    expect(tree[0].item.id).toBe('a')
    expect(tree[1].item.id).toBe('z')
  })
})

describe('findFirstPage', () => {
  it('finds first page in tree', () => {
    const tree = buildTree([
      makeItem({ id: 'f1', parentId: null, type: 'folder', sortOrder: 0 }),
      makeItem({ id: 'p1', parentId: 'f1', type: 'page', sortOrder: 0, path: 'p1' }),
    ])
    const page = findFirstPage(tree[0])
    expect(page?.id).toBe('p1')
  })

  it('returns null if no pages', () => {
    const tree = buildTree([makeItem({ id: 'f1', type: 'folder' })])
    expect(findFirstPage(tree[0])).toBeNull()
  })
})

describe('findPageByPath', () => {
  it('finds page by path', () => {
    const tree = buildTree([
      makeItem({ id: 'p1', type: 'page', path: 'intro' }),
      makeItem({ id: 'p2', type: 'page', path: 'advanced' }),
    ])
    expect(findPageByPath(tree, 'advanced')?.id).toBe('p2')
  })

  it('returns null for missing path', () => {
    expect(findPageByPath([], 'nope')).toBeNull()
  })
})

describe('flattenTree', () => {
  it('returns empty for empty tree', () => {
    expect(flattenTree([])).toEqual([])
  })

  it('flattens nested tree in depth-first order', () => {
    const tree = buildTree([
      makeItem({ id: 'f1', parentId: null, type: 'folder', sortOrder: 0 }),
      makeItem({ id: 'p1', parentId: 'f1', type: 'page', sortOrder: 0 }),
      makeItem({ id: 'p2', parentId: 'f1', type: 'page', sortOrder: 1 }),
      makeItem({ id: 'root', parentId: null, type: 'page', sortOrder: 1 }),
    ])
    const flat = flattenTree(tree)
    expect(flat.map((i) => i.id)).toEqual(['f1', 'p1', 'p2', 'root'])
  })
})
