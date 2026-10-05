import { useTranslation } from 'react-i18next'
import { FALLBACK_LANGUAGE, SUPPORTED_LANGUAGES, isLanguage } from '@/shared/i18n'

export function LanguageToggle() {
  const { t, i18n } = useTranslation()

  const current = isLanguage(i18n.resolvedLanguage) ? i18n.resolvedLanguage : FALLBACK_LANGUAGE
  const next = SUPPORTED_LANGUAGES.find((language) => language !== current) ?? FALLBACK_LANGUAGE

  return (
    <button
      type="button"
      onClick={() => {
        void i18n.changeLanguage(next)
      }}
      aria-label={t('lang.label')}
      className="inline-flex items-center gap-1.5 rounded-full border border-latte px-3 py-1.5 text-sm text-roast transition-colors hover:border-caramel hover:text-caramel-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-caramel"
    >
      <svg className="h-4 w-4" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="12" cy="12" r="8.5" stroke="currentColor" strokeWidth="1.6" />
        <path
          d="M3.5 12h17M12 3.5c2.4 2.3 3.6 5.2 3.6 8.5S14.4 18.2 12 20.5c-2.4-2.3-3.6-5.2-3.6-8.5S9.6 5.8 12 3.5Z"
          stroke="currentColor"
          strokeWidth="1.6"
          strokeLinecap="round"
        />
      </svg>
      {t('lang.switchTo')}
    </button>
  )
}
