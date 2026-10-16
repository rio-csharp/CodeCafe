import { describe, expect, it } from 'vitest'
import type { PageTreeNode } from '@/entities/notebook'
import { dropPositionAt, isSameOrDescendant, resolveDrop } from './dragDrop'

function node(
  id: string,
  path: string,
  children: PageTreeNode[] = [],
): PageTreeNode {
  return {
    id,
    title: id,
    path,
    sortOrder: 0,
    isArchived: false,
    isFavorite: false,
    children,
  }
}

// guide
// ├── setup
// └── advanced
// brew
const TREE: PageTreeNode[] = [
  node('guide', '/guide', [node('setup', '/guide/setup'), node('advanced', '/guide/advanced')]),
  node('brew', '/brew'),
]

describe('dropPositionAt', () => {
  it('maps the row edges to reorder and the middle to nesting', () => {
    expect(dropPositionAt(0)).toBe('before')
    expect(dropPositionAt(0.29)).toBe('before')
    expect(dropPositionAt(0.5)).toBe('inside')
    expect(dropPositionAt(0.71)).toBe('after')
    expect(dropPositionAt(1)).toBe('after')
  })
})

describe('isSameOrDescendant', () => {
  it('matches the node itself and anything below it', () => {
    expect(isSameOrDescendant('/guide', '/guide')).toBe(true)
    expect(isSameOrDescendant('/guide', '/guide/setup')).toBe(true)
    expect(isSameOrDescendant('/guide', '/guide/setup/deep')).toBe(true)
  })

  it('rejects siblings, parents and lookalike prefixes', () => {
    expect(isSameOrDescendant('/guide', '/brew')).toBe(false)
    expect(isSameOrDescendant('/guide/setup', '/guide')).toBe(false)
    // A shared prefix is not descent: /guide-2 is not under /guide.
    expect(isSameOrDescendant('/guide', '/guide-2/setup')).toBe(false)
  })

  it('compares without caring about leading slashes', () => {
    expect(isSameOrDescendant('guide', '/guide/setup')).toBe(true)
  })
})

describe('resolveDrop', () => {
  it('drops onto a node as its last child', () => {
    expect(resolveDrop(TREE, 'brew', 'guide', 'inside')).toEqual({
      parentPath: '/guide',
      afterPageId: 'advanced',
    })
  })

  it('takes the first position when the target has no children', () => {
    expect(resolveDrop(TREE, 'guide', 'brew', 'inside')).toEqual({
      parentPath: '/brew',
      afterPageId: null,
    })
  })

  it('reorders before a sibling, naming the previous sibling to slot after', () => {
    expect(resolveDrop(TREE, 'brew', 'advanced', 'before')).toEqual({
      parentPath: '/guide',
      afterPageId: 'setup',
    })
  })

  it('uses null afterPageId when dropping before the first sibling', () => {
    expect(resolveDrop(TREE, 'brew', 'setup', 'before')).toEqual({
      parentPath: '/guide',
      afterPageId: null,
    })
  })

  it('reorders after a sibling at root level with a null parent', () => {
    expect(resolveDrop(TREE, 'guide', 'brew', 'after')).toEqual({
      parentPath: null,
      afterPageId: 'brew',
    })
  })

  it('refuses to drop a page onto itself or into its own subtree', () => {
    expect(resolveDrop(TREE, 'guide', 'guide', 'inside')).toBeNull()
    expect(resolveDrop(TREE, 'guide', 'setup', 'inside')).toBeNull()
    expect(resolveDrop(TREE, 'guide', 'advanced', 'after')).toBeNull()
  })

  it('refuses reorders that would change nothing', () => {
    // setup already sits right before advanced, and right after... nothing else.
    expect(resolveDrop(TREE, 'setup', 'advanced', 'before')).toBeNull()
    expect(resolveDrop(TREE, 'advanced', 'setup', 'after')).toBeNull()
    // brew is already the last child of nothing — but guide already ends with advanced.
    expect(resolveDrop(TREE, 'advanced', 'guide', 'inside')).toBeNull()
  })

  it('returns null for ids the tree does not know', () => {
    expect(resolveDrop(TREE, 'ghost', 'brew', 'inside')).toBeNull()
    expect(resolveDrop(TREE, 'brew', 'ghost', 'inside')).toBeNull()
  })
})
