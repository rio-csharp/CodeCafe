import { matchRoutes } from 'react-router'
import { describe, expect, it } from 'vitest'
import { router } from './router'

/**
 * One splat route covers the notebook root and every page under it, so the two
 * never mount as separate route entries and switching pages keeps the chrome.
 */
describe('router', () => {
  it('matches the notebook root with an empty page path', () => {
    const matches = matchRoutes(router.routes, '/notebooks/espresso-notes')

    expect(matches?.[0]?.params).toMatchObject({ slug: 'espresso-notes', '*': '' })
  })

  it('matches a nested page path, CJK included', () => {
    const matches = matchRoutes(router.routes, '/notebooks/espresso-notes/guide/深烘-notes')

    expect(matches?.[0]?.params).toMatchObject({ slug: 'espresso-notes', '*': 'guide/深烘-notes' })
  })
})
