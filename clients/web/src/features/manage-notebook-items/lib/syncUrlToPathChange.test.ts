import { describe, expect, it, vi } from 'vitest'
import { syncUrlToPathChange } from './syncUrlToPathChange'

describe('syncUrlToPathChange', () => {
  it('rewrites the URL when it points exactly at the old path', () => {
    const navigate = vi.fn()

    syncUrlToPathChange('old-page', 'new-page', 'slug', '/notes/slug/old-page', navigate)

    expect(navigate).toHaveBeenCalledWith('/notes/slug/new-page', { replace: true })
  })

  it('rewrites descendant paths, preserving the tail below the renamed folder', () => {
    const navigate = vi.fn()

    syncUrlToPathChange(
      'folder/old-name',
      'folder/new-name',
      'slug',
      '/notes/slug/folder/old-name/sub/page',
      navigate,
    )

    expect(navigate).toHaveBeenCalledWith('/notes/slug/folder/new-name/sub/page', { replace: true })
  })

  it('does nothing when the path did not change', () => {
    const navigate = vi.fn()

    syncUrlToPathChange('page', 'page', 'slug', '/notes/slug/page', navigate)

    expect(navigate).not.toHaveBeenCalled()
  })

  it('does nothing when the old path is empty', () => {
    const navigate = vi.fn()

    syncUrlToPathChange('', 'new-page', 'slug', '/notes/slug/new-page', navigate)

    expect(navigate).not.toHaveBeenCalled()
  })

  it('does nothing when the URL is outside the notebook prefix', () => {
    const navigate = vi.fn()

    syncUrlToPathChange('old-page', 'new-page', 'slug', '/notes/other-slug/old-page', navigate)

    expect(navigate).not.toHaveBeenCalled()
  })

  it('does nothing when the URL points at an unrelated path', () => {
    const navigate = vi.fn()

    syncUrlToPathChange('old-page', 'new-page', 'slug', '/notes/slug/another-page', navigate)

    expect(navigate).not.toHaveBeenCalled()
  })

  it('does not rewrite paths that merely share a prefix with the old path', () => {
    const navigate = vi.fn()

    syncUrlToPathChange('old', 'new', 'slug', '/notes/slug/old-but-different/page', navigate)

    expect(navigate).not.toHaveBeenCalled()
  })
})
