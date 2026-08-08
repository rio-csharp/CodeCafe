import { describe, expect, it } from 'vitest'
import { buildTree } from '@/entities/notebook'
import type { NotebookItem } from '@/entities/notebook-item'
import { computeReorderUpdates, type ReorderUpdate } from './computeReorderUpdates'

function makeItem(
  id: string,
  parentId: string | null,
  sortOrder: number,
  type: NotebookItem['type'] = 'page',
): NotebookItem {
  return {
    id,
    notebookId: 'notebook-1',
    parentId,
    type,
    title: id,
    slug: id,
    path: parentId ? `${parentId}/${id}` : id,
    sortOrder,
    contentFormat: type === 'page' ? 'tiptap_json' : null,
    contentJson: null,
    plainTextContent: null,
    isArchived: false,
    archivedAtUtc: null,
    archivedByUserId: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
  }
}

function expectUniqueItemIds(updates: ReorderUpdate[]) {
  const ids = updates.map((u) => u.itemId)
  expect(new Set(ids).size).toBe(ids.length)
}

describe('computeReorderUpdates', () => {
  it('reorders within the same parent without duplicating any itemId', () => {
    const tree = buildTree([
      makeItem('a', null, 0),
      makeItem('b', null, 10),
      makeItem('c', null, 20),
    ])

    const result = computeReorderUpdates(tree, 'a', 'b', 'after')

    expect(result).not.toBeNull()
    expectUniqueItemIds(result!.updates)
    expect(result!.updates).toEqual([
      { itemId: 'b', parentId: null, sortOrder: 0 },
      { itemId: 'a', parentId: null, sortOrder: 10 },
      { itemId: 'c', parentId: null, sortOrder: 20 },
    ])
  })

  it('reorders within the same parent when moving an item upwards', () => {
    const tree = buildTree([
      makeItem('a', null, 0),
      makeItem('b', null, 10),
      makeItem('c', null, 20),
    ])

    const result = computeReorderUpdates(tree, 'c', 'a', 'before')

    expect(result).not.toBeNull()
    expectUniqueItemIds(result!.updates)
    expect(result!.updates).toEqual([
      { itemId: 'c', parentId: null, sortOrder: 0 },
      { itemId: 'a', parentId: null, sortOrder: 10 },
      { itemId: 'b', parentId: null, sortOrder: 20 },
    ])
  })

  it('moves an item across parents and recomputes both sibling lists', () => {
    const tree = buildTree([
      makeItem('a', null, 0),
      makeItem('f', null, 10, 'folder'),
      makeItem('x', 'f', 0),
      makeItem('y', 'f', 10),
    ])

    const result = computeReorderUpdates(tree, 'x', 'a', 'after')

    expect(result).not.toBeNull()
    expectUniqueItemIds(result!.updates)
    expect(result!.updates).toEqual([
      { itemId: 'y', parentId: 'f', sortOrder: 0 },
      { itemId: 'a', parentId: null, sortOrder: 0 },
      { itemId: 'x', parentId: null, sortOrder: 10 },
      { itemId: 'f', parentId: null, sortOrder: 20 },
    ])
  })

  it('dropping inside the current parent lists the dragged item exactly once', () => {
    const tree = buildTree([
      makeItem('f', null, 0, 'folder'),
      makeItem('a', 'f', 0),
      makeItem('b', 'f', 10),
    ])

    const result = computeReorderUpdates(tree, 'a', 'f', 'inside')

    expect(result).not.toBeNull()
    expectUniqueItemIds(result!.updates)
    expect(result!.updates).toEqual([
      { itemId: 'b', parentId: 'f', sortOrder: 0 },
      { itemId: 'a', parentId: 'f', sortOrder: 10 },
    ])
  })

  it('drops into an empty folder on a before/after position intent', () => {
    const tree = buildTree([
      makeItem('a', null, 0),
      makeItem('f', null, 10, 'folder'),
    ])

    const result = computeReorderUpdates(tree, 'a', 'f', 'before')

    expect(result).not.toBeNull()
    expectUniqueItemIds(result!.updates)
    expect(result!.updates).toEqual([
      { itemId: 'f', parentId: null, sortOrder: 0 },
      { itemId: 'a', parentId: 'f', sortOrder: 0 },
    ])
  })

  it('appends at the end when dropping inside a non-empty folder', () => {
    const tree = buildTree([
      makeItem('a', null, 0),
      makeItem('f', null, 10, 'folder'),
      makeItem('x', 'f', 0),
    ])

    const result = computeReorderUpdates(tree, 'a', 'f', 'inside')

    expect(result).not.toBeNull()
    expectUniqueItemIds(result!.updates)
    expect(result!.updates).toEqual([
      { itemId: 'f', parentId: null, sortOrder: 0 },
      { itemId: 'x', parentId: 'f', sortOrder: 0 },
      { itemId: 'a', parentId: 'f', sortOrder: 10 },
    ])
  })

  it('returns null when dropping onto one of the dragged item\'s own descendants', () => {
    const tree = buildTree([
      makeItem('f', null, 0, 'folder'),
      makeItem('g', 'f', 0, 'folder'),
      makeItem('p', 'g', 0),
    ])

    expect(computeReorderUpdates(tree, 'f', 'g', 'inside')).toBeNull()
    expect(computeReorderUpdates(tree, 'f', 'p', 'before')).toBeNull()
  })

  it('returns null when dropping an item inside itself', () => {
    const tree = buildTree([makeItem('f', null, 0, 'folder')])

    expect(computeReorderUpdates(tree, 'f', 'f', 'inside')).toBeNull()
  })

  it('returns null when either node is missing from the tree', () => {
    const tree = buildTree([makeItem('a', null, 0)])

    expect(computeReorderUpdates(tree, 'missing', 'a', 'after')).toBeNull()
    expect(computeReorderUpdates(tree, 'a', 'missing', 'after')).toBeNull()
  })
})
