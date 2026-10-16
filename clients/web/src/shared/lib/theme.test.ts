import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  applyResolvedTheme,
  applyThemeTransition,
  getSystemTheme,
  getThemeMode,
  nextThemeMode,
  setThemeMode,
  subscribeSystemTheme,
  THEME_STORAGE_KEY,
  THEME_SWITCHING_CLASS,
} from './theme'

type Listener = (event: MediaQueryListEvent) => void

const SYSTEM_QUERY = '(prefers-color-scheme: dark)'

/** jsdom ships matchMedia but never fires it, so the tests drive events by hand. */
function stubMatchMedia(initial: boolean) {
  const listeners = new Set<Listener>()
  const media = {
    matches: initial,
    media: SYSTEM_QUERY,
    addEventListener: (_type: string, listener: Listener) => {
      listeners.add(listener)
    },
    removeEventListener: (_type: string, listener: Listener) => {
      listeners.delete(listener)
    },
  }

  window.matchMedia = vi.fn((query: string) =>
    query === SYSTEM_QUERY ? media : { ...media, matches: false, media: query },
  ) as unknown as typeof window.matchMedia

  return {
    flipSystem(next: boolean) {
      media.matches = next
      listeners.forEach((listener) => {
        listener({ matches: next } as MediaQueryListEvent)
      })
    },
    listenerCount: () => listeners.size,
  }
}

beforeEach(() => {
  window.localStorage.clear()
  document.documentElement.classList.remove('dark')
  document.documentElement.classList.remove(THEME_SWITCHING_CLASS)
  document.documentElement.style.removeProperty('--theme-x')
  document.documentElement.style.removeProperty('--theme-y')
  stubMatchMedia(false)
})

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
  delete (document as Partial<Document>).startViewTransition
})

describe('theme mode flipping', () => {
  it('flips light ⇄ dark', () => {
    expect(nextThemeMode('light')).toBe('dark')
    expect(nextThemeMode('dark')).toBe('light')
  })
})

describe('persistence', () => {
  it('treats an absent key as "follow the OS"', () => {
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBeNull()
    expect(getThemeMode()).toBe('light')

    stubMatchMedia(true)
    expect(getThemeMode()).toBe('dark')
  })

  it('treats an unrecognised value as absent', () => {
    window.localStorage.setItem(THEME_STORAGE_KEY, 'sepia')
    stubMatchMedia(true)
    expect(getThemeMode()).toBe('dark')
  })

  it('stores explicit picks', () => {
    expect(setThemeMode('dark')).toBe('dark')
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark')
    expect(getThemeMode()).toBe('dark')

    setThemeMode('light')
    expect(window.localStorage.getItem(THEME_STORAGE_KEY)).toBe('light')
    expect(getThemeMode()).toBe('light')
  })
})

describe('getSystemTheme', () => {
  it('reads prefers-color-scheme', () => {
    stubMatchMedia(true)
    expect(getSystemTheme()).toBe('dark')

    stubMatchMedia(false)
    expect(getSystemTheme()).toBe('light')
  })
})

describe('applyResolvedTheme', () => {
  it('applies the dark class for an explicit dark mode', () => {
    setThemeMode('dark')
    expect(applyResolvedTheme()).toBe('dark')
    expect(document.documentElement.classList.contains('dark')).toBe(true)
  })

  it('removes the dark class for an explicit light mode', () => {
    document.documentElement.classList.add('dark')
    setThemeMode('light')
    expect(applyResolvedTheme()).toBe('light')
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('follows the OS while nothing is stored', () => {
    stubMatchMedia(true)
    expect(applyResolvedTheme()).toBe('dark')
    expect(document.documentElement.classList.contains('dark')).toBe(true)
  })
})

describe('applyThemeTransition', () => {
  const MOTION_QUERY = '(prefers-reduced-motion: reduce)'

  function stubReducedMotion(reduced: boolean) {
    window.matchMedia = vi.fn((query: string) => ({
      matches: query === MOTION_QUERY ? reduced : false,
      media: query,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
    })) as unknown as typeof window.matchMedia
  }

  it('flips instantly when the user prefers reduced motion', () => {
    stubReducedMotion(true)

    applyThemeTransition('dark', { x: 10, y: 20 })

    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(document.documentElement.classList.contains(THEME_SWITCHING_CLASS)).toBe(false)
    expect(document.documentElement.style.getPropertyValue('--theme-x')).toBe('')
  })

  it('falls back to a temporary crossfade class without the View Transitions API', () => {
    vi.useFakeTimers()
    stubReducedMotion(false)

    applyThemeTransition('dark')

    // The theme is already applied; the class only buys the 300ms blend.
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(document.documentElement.classList.contains(THEME_SWITCHING_CLASS)).toBe(true)

    vi.advanceTimersByTime(300)
    expect(document.documentElement.classList.contains(THEME_SWITCHING_CLASS)).toBe(false)
  })

  it('replays the theme inside a view transition from the given origin', () => {
    stubReducedMotion(false)
    const startViewTransition = vi.fn((callback: () => void) => {
      callback()
      return { finished: Promise.resolve() }
    })
    Object.defineProperty(document, 'startViewTransition', {
      value: startViewTransition,
      configurable: true,
    })

    applyThemeTransition('dark', { x: 12, y: 34 })

    expect(startViewTransition).toHaveBeenCalledTimes(1)
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(document.documentElement.style.getPropertyValue('--theme-x')).toBe('12px')
    expect(document.documentElement.style.getPropertyValue('--theme-y')).toBe('34px')
    expect(document.documentElement.classList.contains(THEME_SWITCHING_CLASS)).toBe(false)
  })

  it('persists the mode regardless of the animation path', () => {
    stubReducedMotion(true)

    applyThemeTransition('dark')

    expect(getThemeMode()).toBe('dark')
  })
})

describe('subscribeSystemTheme', () => {
  it('reacts to OS changes while in system mode', () => {
    const media = stubMatchMedia(false)
    const onChange = vi.fn()

    subscribeSystemTheme(onChange)
    media.flipSystem(true)

    expect(onChange).toHaveBeenCalledTimes(1)
    expect(onChange).toHaveBeenCalledWith('dark')
  })

  it('ignores OS changes once an explicit mode is picked', () => {
    const media = stubMatchMedia(false)
    const onChange = vi.fn()

    subscribeSystemTheme(onChange)
    setThemeMode('dark')
    media.flipSystem(true)

    expect(onChange).not.toHaveBeenCalled()
  })

  it('detaches on unsubscribe', () => {
    const media = stubMatchMedia(false)
    const onChange = vi.fn()

    const unsubscribe = subscribeSystemTheme(onChange)
    expect(media.listenerCount()).toBe(1)

    unsubscribe()
    expect(media.listenerCount()).toBe(0)

    media.flipSystem(true)
    expect(onChange).not.toHaveBeenCalled()
  })
})
