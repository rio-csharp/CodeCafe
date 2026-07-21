import { beforeEach, describe, expect, it } from 'vitest'
import {
  AI_EDIT_THREAD_STORAGE_PREFIX,
  AI_THREAD_STORAGE_PREFIX,
  clearLocalStorageByPrefix,
} from './storageKeys'

describe('clearLocalStorageByPrefix', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('removes only keys matching the prefix', () => {
    localStorage.setItem(`${AI_THREAD_STORAGE_PREFIX}t1`, 'a')
    localStorage.setItem(`${AI_THREAD_STORAGE_PREFIX}t2`, 'b')
    localStorage.setItem(`${AI_EDIT_THREAD_STORAGE_PREFIX}t1`, 'c')
    localStorage.setItem('codecafe:other', 'd')

    clearLocalStorageByPrefix(AI_THREAD_STORAGE_PREFIX)

    expect(localStorage.getItem(`${AI_THREAD_STORAGE_PREFIX}t1`)).toBeNull()
    expect(localStorage.getItem(`${AI_THREAD_STORAGE_PREFIX}t2`)).toBeNull()
    expect(localStorage.getItem(`${AI_EDIT_THREAD_STORAGE_PREFIX}t1`)).toBe('c')
    expect(localStorage.getItem('codecafe:other')).toBe('d')
  })

  it('distinguishes overlapping prefixes', () => {
    // AI_THREAD_STORAGE_PREFIX is a prefix of... nothing here, but the two
    // AI prefixes share a common stem; clearing one must not touch the other.
    localStorage.setItem(`${AI_EDIT_THREAD_STORAGE_PREFIX}x`, 'e')

    clearLocalStorageByPrefix(AI_THREAD_STORAGE_PREFIX)

    expect(localStorage.getItem(`${AI_EDIT_THREAD_STORAGE_PREFIX}x`)).toBe('e')
  })

  it('is a no-op when no key matches', () => {
    localStorage.setItem('unrelated', 'value')

    clearLocalStorageByPrefix('codecafe:missing:')

    expect(localStorage.getItem('unrelated')).toBe('value')
    expect(localStorage.length).toBe(1)
  })

  it('is a no-op on empty storage', () => {
    expect(() => clearLocalStorageByPrefix(AI_THREAD_STORAGE_PREFIX)).not.toThrow()
    expect(localStorage.length).toBe(0)
  })
})
