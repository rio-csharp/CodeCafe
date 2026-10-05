export type ThemeMode = 'light' | 'dark'

export const THEME_STORAGE_KEY = 'codecafe.theme'

/** The toggle flips between these two; the OS preference is only the initial default. */
export const THEME_MODES = ['light', 'dark'] as const

export const DARK_CLASS = 'dark'

const SYSTEM_QUERY = '(prefers-color-scheme: dark)'

export function isThemeMode(value: unknown): value is ThemeMode {
  return THEME_MODES.some((mode) => mode === value)
}

function readStoredMode(): ThemeMode | null {
  try {
    const raw = window.localStorage.getItem(THEME_STORAGE_KEY)
    return isThemeMode(raw) ? raw : null
  } catch {
    // Private mode / disabled storage: fall back to the OS rather than breaking.
    return null
  }
}

export function getSystemTheme(): ThemeMode {
  if (typeof window.matchMedia !== 'function') {
    return 'light'
  }
  return window.matchMedia(SYSTEM_QUERY).matches ? 'dark' : 'light'
}

/** The stored choice, or the OS preference while the user has never picked one. */
export function getThemeMode(): ThemeMode {
  return readStoredMode() ?? getSystemTheme()
}

export function setThemeMode(mode: ThemeMode): ThemeMode {
  try {
    window.localStorage.setItem(THEME_STORAGE_KEY, mode)
  } catch {
    // Keep the in-memory toggle usable even when the choice cannot persist.
  }
  return mode
}

/** Writes the current theme to <html> as a class plus a native color-scheme hint. */
export function applyResolvedTheme(): ThemeMode {
  const resolved = getThemeMode()
  const root = document.documentElement

  root.classList.toggle(DARK_CLASS, resolved === 'dark')
  root.style.colorScheme = resolved

  return resolved
}

export function nextThemeMode(mode: ThemeMode): ThemeMode {
  return mode === 'light' ? 'dark' : 'light'
}

/** Temporarily opted onto <html> for the fallback crossfade only. */
export const THEME_SWITCHING_CLASS = 'theme-switching'

const FALLBACK_TRANSITION_MS = 300
const MOTION_QUERY = '(prefers-reduced-motion: reduce)'

export interface ThemeTransitionOrigin {
  x: number
  y: number
}

function prefersReducedMotion(): boolean {
  return (
    typeof window.matchMedia === 'function' && window.matchMedia(MOTION_QUERY).matches
  )
}

/**
 * Persists the mode and applies it with the smoothest animation available: a
 * circular view-transition reveal from the toggle, a brief crossfade where the
 * View Transitions API is missing, or an instant flip under reduced motion.
 */
export function applyThemeTransition(mode: ThemeMode, origin?: ThemeTransitionOrigin): void {
  setThemeMode(mode)

  if (prefersReducedMotion()) {
    applyResolvedTheme()
    return
  }

  if (typeof document.startViewTransition === 'function') {
    const root = document.documentElement
    if (origin) {
      root.style.setProperty('--theme-x', `${origin.x}px`)
      root.style.setProperty('--theme-y', `${origin.y}px`)
    }
    document.startViewTransition(() => {
      applyResolvedTheme()
    })
    return
  }

  // Fallback: opt every element into a short color transition, then remove it so
  // later state changes (hover, skeleton pulses) are not slowed down.
  const root = document.documentElement
  root.classList.add(THEME_SWITCHING_CLASS)
  applyResolvedTheme()
  window.setTimeout(() => {
    root.classList.remove(THEME_SWITCHING_CLASS)
  }, FALLBACK_TRANSITION_MS)
}

/**
 * Follows OS theme changes, but only while the user has never picked a mode
 * explicitly. Ignored rather than unsubscribed at event time, so clearing the
 * choice would re-activate the same subscription.
 */
export function subscribeSystemTheme(onChange: (theme: ThemeMode) => void): () => void {
  if (typeof window.matchMedia !== 'function') {
    return () => undefined
  }

  const media = window.matchMedia(SYSTEM_QUERY)
  const handleChange = (event: MediaQueryListEvent) => {
    if (readStoredMode() !== null) {
      return
    }
    onChange(event.matches ? 'dark' : 'light')
  }

  media.addEventListener('change', handleChange)
  return () => {
    media.removeEventListener('change', handleChange)
  }
}
