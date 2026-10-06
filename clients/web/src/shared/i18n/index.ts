import i18n from 'i18next'
import LanguageDetector from 'i18next-browser-languagedetector'
import { initReactI18next } from 'react-i18next'
import { en } from './en'
import { zh } from './zh'

export const SUPPORTED_LANGUAGES = ['en', 'zh'] as const
export type Language = (typeof SUPPORTED_LANGUAGES)[number]

/** One of the three localStorage keys the app owns: lang, theme, refresh. */
export const LANGUAGE_STORAGE_KEY = 'codecafe.lang'

export const FALLBACK_LANGUAGE: Language = 'en'

export function isLanguage(value: string | null | undefined): value is Language {
  return SUPPORTED_LANGUAGES.some((language) => language === value)
}

void i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      zh: { translation: zh },
    },
    fallbackLng: FALLBACK_LANGUAGE,
    supportedLngs: [...SUPPORTED_LANGUAGES],
    nonExplicitSupportedLngs: true,
    interpolation: { escapeValue: false },
    detection: {
      order: ['localStorage', 'navigator'],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
      caches: ['localStorage'],
    },
  })

export { i18n }
export default i18n
