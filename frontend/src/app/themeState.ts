export type Theme = 'dark' | 'light'

export const storageKey = 'codecafe-theme'

export const themeColorByTheme: Record<Theme, string> = {
  dark: '#080b14',
  light: '#f4f7fb',
}

export function readInitialTheme(): Theme {
  if (typeof window === 'undefined') {
    return 'dark'
  }

  const stored = window.localStorage.getItem(storageKey)

  if (stored === 'dark' || stored === 'light') {
    return stored
  }

  return 'dark'
}
