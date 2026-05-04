import { createContext } from 'react'
import type { Theme } from './themeState'

export type ThemeContextValue = {
  setTheme: (theme: Theme) => void
  theme: Theme
}

export const ThemeContext = createContext<ThemeContextValue | null>(null)
