import { afterEach, describe, expect, it, vi } from 'vitest'
import { randomId } from './randomId'

afterEach(() => vi.unstubAllGlobals())

describe('randomId', () => {
  it('uses the native generator in a secure context', () => {
    const native = vi.fn(() => 'native-id')
    vi.stubGlobal('crypto', { randomUUID: native })
    expect(randomId()).toBe('native-id')
    expect(native).toHaveBeenCalledOnce()
  })

  it('generates v4 UUIDs on LAN HTTP without randomUUID', () => {
    const getRandomValues = vi.fn((bytes: Uint8Array) => {
      bytes.fill(255)
      return bytes
    })
    vi.stubGlobal('crypto', { getRandomValues })
    expect(randomId()).toBe('ffffffff-ffff-4fff-bfff-ffffffffffff')
    expect(getRandomValues).toHaveBeenCalledOnce()
  })
})
