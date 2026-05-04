import {
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { ThemeContext, type ThemeContextValue } from './themeContext'
import { readInitialTheme, storageKey, themeColorByTheme, type Theme } from './themeState'

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
