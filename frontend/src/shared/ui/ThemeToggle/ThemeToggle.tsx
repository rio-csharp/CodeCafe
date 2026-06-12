import { Sun, Moon, Monitor } from 'lucide-react'
import { useThemeStore } from '@/shared/model/themeStore'
import { useState, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { useClickOutside } from '@/shared/hooks/useClickOutside'

function useThemeOptions() {
  const { t } = useTranslation()
  return [
    { value: 'light' as const, label: t('theme.light'), icon: Sun },
    { value: 'dark' as const, label: t('theme.dark'), icon: Moon },
    { value: 'system' as const, label: t('theme.system'), icon: Monitor },
  ]
}

interface ThemeToggleProps {
  placement?: 'top' | 'bottom'
  align?: 'left' | 'right'
}

export function ThemeToggle({ placement = 'bottom', align = 'right' }: ThemeToggleProps) {
  const { t } = useTranslation()
  const { theme, setTheme } = useThemeStore()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  useClickOutside(ref, () => setOpen(false))

  const options = useThemeOptions()
  const active = options.find((o) => o.value === theme) ?? options[2]
  const Icon = active.icon

  const horizontalAlign = align === 'left' ? 'left-0' : 'right-0'
  const menuPositionClasses =
    placement === 'top'
      ? `absolute ${horizontalAlign} bottom-full mb-2`
      : `absolute ${horizontalAlign} mt-2`

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen(!open)}
        className="flex items-center justify-center h-8 w-8 rounded-lg hover:bg-surface-hover dark:hover:bg-surface-active transition-colors"
        aria-label={t('theme.toggle')}
        title={t('theme.toggle')}
      >
        <Icon className="h-4 w-4 text-text-secondary" />
      </button>
      {open && (
        <div className={`${menuPositionClasses} w-36 rounded-xl border border-border-default bg-surface shadow-lg py-1 z-50`}>
          {options.map((o) => {
            const OIcon = o.icon
            return (
              <button
                type="button"
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
