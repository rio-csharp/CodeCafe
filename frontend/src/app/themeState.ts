export type Theme = 'dark' | 'light' | 'e-ink'

export const storageKey = 'codecafe-theme'

export const themeColorByTheme: Record<Theme, string> = {
  dark: '#080b14',
  'e-ink': '#f3f1ea',
  light: '#f4f7fb',
}

export function readInitialTheme(): Theme {
  if (typeof window === 'undefined') {
    return 'dark'
  }

  const stored = window.localStorage.getItem(storageKey)

  if (stored === 'dark' || stored === 'light' || stored === 'e-ink') {
    return stored
  }

  return 'dark'
}
