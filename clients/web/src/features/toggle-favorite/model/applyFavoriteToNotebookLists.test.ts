import type { InfiniteData } from '@tanstack/react-query'
import { describe, expect, it } from 'vitest'
import type { Notebook } from '@/entities/notebook'
import { applyFavoriteToNotebookLists } from './applyFavoriteToNotebookLists'

function makeNotebook(id: string, overrides: Partial<Notebook> = {}): Notebook {
  return {
    id,
    ownerId: 'owner-1',
    title: `Notebook ${id}`,
    slug: `notebook-${id}`,
    description: null,
    visibility: 'public',
    isPublished: true,
    authorDisplayName: 'Author',
    itemCount: 0,
    folderCount: 0,
    pageCount: 0,
    favoriteCount: 3,
    isFavoritedByMe: false,
    lastActivityAtUtc: '2026-01-01T00:00:00Z',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: null,
    publishedAtUtc: null,
    ...overrides,
  }
}

function makeInfiniteData(...pages: Notebook[][]): InfiniteData<Notebook[]> {
  return { pages, pageParams: pages.map((_, index) => index) }
}

describe('applyFavoriteToNotebookLists', () => {
  it('flips isFavoritedByMe and increments favoriteCount on the matching notebook', () => {
    const target = makeNotebook('b')
    const data = makeInfiniteData([makeNotebook('a'), target], [makeNotebook('c')])

    const result = applyFavoriteToNotebookLists(data, 'b')

    const flipped = result?.pages[0][1]
    expect(flipped?.isFavoritedByMe).toBe(true)
    expect(flipped?.favoriteCount).toBe(4)
    // Untouched notebooks keep their identity; input is not mutated.
    expect(result?.pages[0][0]).toBe(data.pages[0][0])
    expect(result?.pages[1]).toBe(data.pages[1])
    expect(target.isFavoritedByMe).toBe(false)
  })

  it('decrements favoriteCount when un-favoriting', () => {
    const data = makeInfiniteData([makeNotebook('a', { isFavoritedByMe: true, favoriteCount: 3 })])

    const result = applyFavoriteToNotebookLists(data, 'a')

    expect(result?.pages[0][0].isFavoritedByMe).toBe(false)
    expect(result?.pages[0][0].favoriteCount).toBe(2)
  })

  it('toggles a notebook on a later page', () => {
    const data = makeInfiniteData([makeNotebook('a')], [makeNotebook('b')])

    const result = applyFavoriteToNotebookLists(data, 'b')

    expect(result?.pages[0]).toBe(data.pages[0])
    expect(result?.pages[1][0].isFavoritedByMe).toBe(true)
  })

  it('returns the same reference when no notebook matches', () => {
    const data = makeInfiniteData([makeNotebook('a')])

    expect(applyFavoriteToNotebookLists(data, 'missing')).toBe(data)
  })

  it('returns undefined for an uncached list', () => {
    expect(applyFavoriteToNotebookLists(undefined, 'a')).toBeUndefined()
  })
})
