import { create } from 'zustand'
import { persist } from 'zustand/middleware'

type Theme = 'light' | 'dark' | 'system'

interface ThemeState {
  theme: Theme
  resolvedTheme: 'light' | 'dark'
  setTheme: (theme: Theme) => void
  init: () => void
}

function getSystemTheme(): 'light' | 'dark' {
  if (typeof window === 'undefined') return 'light'
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function applyTheme(theme: Theme) {
  const resolved = theme === 'system' ? getSystemTheme() : theme
  const root = document.documentElement
  if (resolved === 'dark') {
    root.classList.add('dark')
  } else {
    root.classList.remove('dark')
  }
  return resolved
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'system',
      resolvedTheme: 'light',
      setTheme: (theme) => {
        const resolved = applyTheme(theme)
        set({ theme, resolvedTheme: resolved })
      },
      init: () => {
        const { theme } = get()
        const resolved = applyTheme(theme)
        set({ resolvedTheme: resolved })

        const listener = (e: MediaQueryListEvent) => {
          if (get().theme === 'system') {
            const r = e.matches ? 'dark' : 'light'
            applyTheme('system')
            set({ resolvedTheme: r })
          }
        }
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', listener)
      },
    }),
    {
      name: 'codecafe-theme',
    }
  )
)
