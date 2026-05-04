import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'

export type Theme = 'dark' | 'light'

type ThemeContextValue = {
  setTheme: (theme: Theme) => void
  theme: Theme
}

const storageKey = 'codecafe-theme'
const themeColorByTheme: Record<Theme, string> = {
  dark: '#080b14',
  light: '#f4f7fb',
}

const ThemeContext = createContext<ThemeContextValue | null>(null)

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>(() => readInitialTheme())

  useEffect(() => {
    document.documentElement.dataset.theme = theme
    window.localStorage.setItem(storageKey, theme)
    document
      .querySelector('meta[name="theme-color"]')
      ?.setAttribute('content', themeColorByTheme[theme])
  }, [theme])

  const value = useMemo<ThemeContextValue>(
    () => ({
      setTheme,
      theme,
    }),
    [theme],
  )

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme() {
  const context = useContext(ThemeContext)

  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider')
  }

  return context
}

function readInitialTheme(): Theme {
  if (typeof window === 'undefined') {
    return 'dark'
  }

  const stored = window.localStorage.getItem(storageKey)

  if (stored === 'dark' || stored === 'light') {
    return stored
  }

  return 'dark'
}
