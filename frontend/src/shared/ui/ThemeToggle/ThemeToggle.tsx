import { Sun, Moon, Monitor } from 'lucide-react'
import { useThemeStore } from '@/shared/model/themeStore'
import { useState, useRef } from 'react'
import { useClickOutside } from '@/shared/hooks/useClickOutside'

const options = [
  { value: 'light' as const, label: 'Light', icon: Sun },
  { value: 'dark' as const, label: 'Dark', icon: Moon },
  { value: 'system' as const, label: 'System', icon: Monitor },
]

export function ThemeToggle() {
  const { theme, setTheme } = useThemeStore()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  useClickOutside(ref, () => setOpen(false))

  const active = options.find((o) => o.value === theme) ?? options[2]
  const Icon = active.icon

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center justify-center h-8 w-8 rounded-lg hover:bg-surface-hover dark:hover:bg-surface-active transition-colors"
        aria-label="Toggle theme"
        title="Toggle theme"
      >
        <Icon className="h-4 w-4 text-text-secondary" />
      </button>
      {open && (
        <div className="absolute right-0 mt-2 w-36 rounded-xl border border-border-default bg-surface shadow-lg py-1 z-50">
          {options.map((o) => {
            const OIcon = o.icon
            return (
              <button
                key={o.value}
                onClick={() => {
                  setTheme(o.value)
                  setOpen(false)
                }}
                className={`flex items-center gap-2 w-full px-3 py-2 text-sm transition-colors ${
                  theme === o.value
                    ? 'text-text-primary font-medium bg-surface-active'
                    : 'text-text-secondary hover:bg-surface-hover'
                }`}
              >
                <OIcon className="h-4 w-4" />
                {o.label}
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}
