import { beforeEach, describe, expect, it } from 'vitest'
import {
  ACCESS_CODE_HEADER,
  accessCodeForPath,
  clearAccessCode,
  getAccessCode,
  setAccessCode,
} from './accessCodes'

beforeEach(() => {
  window.sessionStorage.clear()
})

describe('access code store', () => {
  it('round-trips a code under the per-slug sessionStorage key', () => {
    setAccessCode('espresso-notes', 'let-me-in')

    expect(getAccessCode('espresso-notes')).toBe('let-me-in')
    expect(window.sessionStorage.getItem('codecafe.access.espresso-notes')).toBe('let-me-in')
  })

  it('keeps codes of different notebooks apart', () => {
    setAccessCode('a', 'code-a')
    setAccessCode('b', 'code-b')

    expect(getAccessCode('a')).toBe('code-a')
    expect(getAccessCode('b')).toBe('code-b')
  })

  it('returns null for a notebook without a code and after clearing', () => {
    expect(getAccessCode('nothing')).toBeNull()

    setAccessCode('espresso-notes', 'let-me-in')
    clearAccessCode('espresso-notes')

    expect(getAccessCode('espresso-notes')).toBeNull()
    expect(window.sessionStorage.getItem('codecafe.access.espresso-notes')).toBeNull()
  })

  it('exports the wire header name the server reads', () => {
    expect(ACCESS_CODE_HEADER).toBe('X-CodeCafe-Access-Code')
  })
})

describe('accessCodeForPath', () => {
  it('finds the stored code for a notebook read path', () => {
    setAccessCode('espresso-notes', 'let-me-in')

    expect(accessCodeForPath('/api/notebooks/espresso-notes/tree')).toEqual({
      slug: 'espresso-notes',
      code: 'let-me-in',
    })
    expect(accessCodeForPath('/api/notebooks/espresso-notes')).toEqual({
      slug: 'espresso-notes',
      code: 'let-me-in',
    })
    expect(accessCodeForPath('/api/notebooks/espresso-notes/pages/by-path?path=%2Fa')).toEqual({
      slug: 'espresso-notes',
      code: 'let-me-in',
    })
  })

  it('decodes the slug segment, so CJK notebooks match their stored code', () => {
    setAccessCode('我的笔记本', '芝麻开门')

    expect(accessCodeForPath('/api/notebooks/%E6%88%91%E7%9A%84%E7%AC%94%E8%AE%B0%E6%9C%AC/tree')).toEqual({
      slug: '我的笔记本',
      code: '芝麻开门',
    })
  })

  it('ignores paths outside the notebook namespace', () => {
    setAccessCode('espresso-notes', 'let-me-in')

    expect(accessCodeForPath('/api/notebooks')).toBeNull()
    expect(accessCodeForPath('/api/pages/page-1/export')).toBeNull()
    expect(accessCodeForPath('/api/auth/me')).toBeNull()
  })

  it('returns null when no code is stored for the notebook', () => {
    expect(accessCodeForPath('/api/notebooks/espresso-notes/tree')).toBeNull()
  })
})
