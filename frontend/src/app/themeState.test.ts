import { describe, expect, it } from 'vitest'
import { readInitialTheme, themeColorByTheme } from './themeState'

describe('themeState', () => {
  it('returns the stored theme when it is valid', () => {
    window.localStorage.setItem('codecafe-theme', 'light')

    expect(readInitialTheme()).toBe('light')
  })

  it('falls back to dark when the stored theme is invalid', () => {
    window.localStorage.setItem('codecafe-theme', 'sepia')

    expect(readInitialTheme()).toBe('dark')
  })

  it('exposes a theme color for each theme', () => {
    expect(themeColorByTheme.dark).toBe('#080b14')
    expect(themeColorByTheme.light).toBe('#f4f7fb')
    expect(themeColorByTheme['e-ink']).toBe('#f3f1ea')
  })
})
