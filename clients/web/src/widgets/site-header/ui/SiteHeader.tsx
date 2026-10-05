import { useTranslation } from 'react-i18next'
import { LanguageToggle } from '@/features/switch-language'

export function SiteHeader() {
  const { t } = useTranslation()

  return (
    <header className="sticky top-0 z-10 border-b border-latte bg-paper">
      <div className="mx-auto flex w-full max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
        <div className="flex items-center gap-2">
          <svg className="h-7 w-7 text-caramel" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path
              d="M4 8h11v6a4 4 0 0 1-4 4H8a4 4 0 0 1-4-4V8Z"
              stroke="currentColor"
              strokeWidth="1.6"
              strokeLinejoin="round"
            />
            <path
              d="M15 9.5h2.5a2.5 2.5 0 0 1 0 5H15"
              stroke="currentColor"
              strokeWidth="1.6"
              strokeLinecap="round"
            />
            <path
              d="M7.5 4.5c0-1 1-1.4 1-2.5M11 4.5c0-1 1-1.4 1-2.5"
              stroke="currentColor"
              strokeWidth="1.4"
              strokeLinecap="round"
            />
          </svg>
          <span className="font-display text-lg tracking-tight text-espresso">
            {t('brand.name')}
          </span>
        </div>

        <LanguageToggle />
      </div>
    </header>
  )
}
