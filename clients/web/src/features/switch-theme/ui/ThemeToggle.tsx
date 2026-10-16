import { useEffect, useState } from 'react'
import type { MouseEvent, ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import {
  applyResolvedTheme,
  applyThemeTransition,
  getThemeMode,
  nextThemeMode,
  subscribeSystemTheme,
} from '@/shared/lib'
import type { ThemeMode } from '@/shared/lib'
import { DarkIcon, LightIcon } from './icons'

const THEME_ICONS: Record<ThemeMode, () => ReactElement> = {
  light: LightIcon,
  dark: DarkIcon,
}

export function ThemeToggle() {
  const { t } = useTranslation()
  const [mode, setMode] = useState<ThemeMode>(getThemeMode)

  useEffect(() => {
    // Mount-only: sync with the inline script's decision. Later flips are applied by
    // applyThemeTransition (explicit picks) or the system subscription (OS changes).
    applyResolvedTheme()

    // OS changes only land while in system mode.
    return subscribeSystemTheme(() => {
      applyResolvedTheme()
    })
  }, [])

  const cycle = (event: MouseEvent<HTMLButtonElement>) => {
    const next = nextThemeMode(mode)
    applyThemeTransition(next, { x: event.clientX, y: event.clientY })
    setMode(next)
  }

  const Icon = THEME_ICONS[mode]
  const modeLabel = t(`theme.${mode}`)

  return (
    <button
      type="button"
      onClick={cycle}
      aria-label={t('theme.labelWithMode', { mode: modeLabel })}
      title={t('theme.labelWithMode', { mode: modeLabel })}
      data-testid="theme-toggle"
      className="inline-flex items-center gap-1.5 rounded-full border border-line px-3 py-1.5 text-sm text-ink transition-colors hover:border-accent hover:text-accent-strong focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
    >
      <span data-testid={`theme-icon-${mode}`} className="inline-flex">
        <Icon />
      </span>
      <span className="sr-only sm:not-sr-only">{modeLabel}</span>
    </button>
  )
}
