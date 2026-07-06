import { useState, useRef } from 'react'
import { Globe } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useClickOutside } from '@/shared/hooks/useClickOutside'

interface LanguageSwitcherProps {
  placement?: 'top' | 'bottom'
  align?: 'left' | 'right'
}

export function LanguageSwitcher({ placement = 'bottom', align = 'right' }: LanguageSwitcherProps) {
  const { t, i18n } = useTranslation()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  useClickOutside(ref, () => setOpen(false))

  const locales = [
    { code: 'en', label: t('language.en') },
    { code: 'zh', label: t('language.zh') },
  ] as const

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
        aria-label={t('language.switch')}
        title={t('language.switch')}
      >
        <Globe className="h-4 w-4 text-text-secondary" />
      </button>
      {open && (
        <div className={`${menuPositionClasses} w-36 rounded-xl border border-border-default bg-surface shadow-lg py-1 z-50`}>
          {locales.map((l) => (
            <button
              type="button"
              key={l.code}
              onClick={() => {
                i18n.changeLanguage(l.code)
                setOpen(false)
              }}
              className={`flex items-center gap-2 w-full px-3 py-2 text-sm transition-colors ${
                i18n.language === l.code
                  ? 'text-text-primary font-medium bg-surface-active'
                  : 'text-text-secondary hover:bg-surface-hover'
              }`}
            >
              <span className="text-xs font-medium w-5">{l.code.toUpperCase()}</span>
              {l.label}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
