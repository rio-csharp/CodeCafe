import { afterEach, describe, expect, it } from 'vitest'
import { apiUrl, getApiBaseUrl } from './baseUrl'

function setConfig(config: CodeCafeConfig | undefined): void {
  if (config === undefined) {
    delete window.__CODECAFE_CONFIG__
    return
  }
  window.__CODECAFE_CONFIG__ = config
}

afterEach(() => {
  setConfig(undefined)
})

describe('getApiBaseUrl', () => {
  it('is empty without runtime configuration', () => {
    expect(getApiBaseUrl()).toBe('')
  })

  it('trims whitespace and trailing slashes', () => {
    setConfig({ apiBaseUrl: '  https://api.example.test//  ' })
    expect(getApiBaseUrl()).toBe('https://api.example.test')
  })

  it('ignores a non-string value', () => {
    window.__CODECAFE_CONFIG__ = { apiBaseUrl: undefined }
    expect(getApiBaseUrl()).toBe('')
  })
})

describe('apiUrl', () => {
  it('returns the path unchanged while same-origin', () => {
    expect(apiUrl('/api/notebooks/public')).toBe('/api/notebooks/public')
  })

  it('prefixes the configured origin', () => {
    setConfig({ apiBaseUrl: 'https://api.example.test' })
    expect(apiUrl('/api/notebooks/public')).toBe('https://api.example.test/api/notebooks/public')
  })

  it('adds the separator for a path without a leading slash', () => {
    setConfig({ apiBaseUrl: 'https://api.example.test' })
    expect(apiUrl('api/notebooks/public')).toBe('https://api.example.test/api/notebooks/public')
  })
})
