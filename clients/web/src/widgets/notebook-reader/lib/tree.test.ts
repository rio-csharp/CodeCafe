import { describe, expect, it } from 'vitest'
import type { PageTreeNode } from '@/entities/notebook'
import { pageNeighbours, treeDescendantCount, visibleTree } from './tree'

function node(
  id: string,
  path: string,
  isArchived = false,
  children: PageTreeNode[] = [],
): PageTreeNode {
  return {
    id,
    title: id,
    path,
    sortOrder: 0,
    isArchived,
    isFavorite: false,
    children,
  }
}

describe('visibleTree', () => {
  const roots: PageTreeNode[] = [
    node('guide', '/guide', false, [
      node('setup', '/guide/setup'),
      node('retired', '/guide/retired', true),
    ]),
    node('stash', '/stash', true),
  ]

  it('hides archived pages and everything under them by default', () => {
    const visible = visibleTree(roots)

    expect(visible.map((entry) => entry.id)).toEqual(['guide'])
    expect(visible[0]?.children.map((entry) => entry.id)).toEqual(['setup'])
  })

  it('keeps archived pages when includeArchived is set', () => {
    const visible = visibleTree(roots, { includeArchived: true })

    expect(visible.map((entry) => entry.id)).toEqual(['guide', 'stash'])
    expect(visible[0]?.children.map((entry) => entry.id)).toEqual(['setup', 'retired'])
  })
})

const PAGES = [
  { path: '/guide', title: 'Guide' },
  { path: '/guide/setup', title: 'Setup' },
  { path: '/brew', title: 'Brew' },
]

describe('treeDescendantCount', () => {
  const roots: PageTreeNode[] = [
    node('guide', '/guide', false, [
      node('setup', '/guide/setup', false, [node('rust', '/guide/setup/rust')]),
      node('retired', '/guide/retired', true),
    ]),
    node('loose', '/loose'),
  ]

  it('counts the whole subtree, archived pages included', () => {
    expect(treeDescendantCount(roots, 'guide')).toBe(3)
  })

  it('counts a nested subtree on its own', () => {
    expect(treeDescendantCount(roots, 'setup')).toBe(1)
  })

  it('reports a leaf and an unknown page as childless', () => {
    // A page created moments ago is missing from the tree until the refetch lands, and a
    // fresh page cannot have children.
    expect(treeDescendantCount(roots, 'loose')).toBe(0)
    expect(treeDescendantCount(roots, 'brand-new')).toBe(0)
  })
})

describe('pageNeighbours', () => {
  it('points at the linear neighbours of the open page', () => {
    const { prev, next } = pageNeighbours(PAGES, 'guide/setup')

    expect(prev?.title).toBe('Guide')
    expect(next?.title).toBe('Brew')
  })

  // Tree paths carry a leading slash, the route splat does not; a raw
  // comparison never matches and the pills silently vanish.
  it('matches regardless of the leading slash', () => {
    expect(pageNeighbours(PAGES, 'brew').prev?.title).toBe('Setup')
    expect(pageNeighbours(PAGES, '/brew').prev?.title).toBe('Setup')
  })

  it('has no prev on the first page and no next on the last', () => {
    expect(pageNeighbours(PAGES, 'guide')).toEqual({
      prev: null,
      next: expect.objectContaining({ title: 'Setup' }),
    })
    expect(pageNeighbours(PAGES, 'brew')).toEqual({
      prev: expect.objectContaining({ title: 'Setup' }),
      next: null,
    })
  })

  it('reports nothing on the notebook root or for a stale path', () => {
    expect(pageNeighbours(PAGES, null)).toEqual({ prev: null, next: null })
    expect(pageNeighbours(PAGES, 'gone')).toEqual({ prev: null, next: null })
  })
})
