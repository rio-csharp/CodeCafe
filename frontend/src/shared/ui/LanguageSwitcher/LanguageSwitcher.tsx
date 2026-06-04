import { useState, useRef } from 'react'
import { Globe } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useClickOutside } from '@/shared/hooks/useClickOutside'

const locales = [
  { code: 'en', label: 'English' },
  { code: 'zh', label: '中文' },
] as const

export function LanguageSwitcher() {
  const { i18n } = useTranslation()
  const [open, setOpen] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  useClickOutside(ref, () => setOpen(false))

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen(!open)}
        className="flex items-center justify-center h-8 w-8 rounded-lg hover:bg-surface-hover dark:hover:bg-surface-active transition-colors"
        aria-label="Switch language"
        title="Switch language"
      >
        <Globe className="h-4 w-4 text-text-secondary" />
      </button>
      {open && (
        <div className="absolute right-0 mt-2 w-36 rounded-xl border border-border-default bg-surface shadow-lg py-1 z-50">
          {locales.map((l) => (
            <button
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
