import { describe, expect, it } from 'vitest'
import { pageNeighbours } from './tree'

const PAGES = [
  { path: '/guide', title: 'Guide' },
  { path: '/guide/setup', title: 'Setup' },
  { path: '/brew', title: 'Brew' },
]

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
