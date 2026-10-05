import { useTranslation } from 'react-i18next'

export function SiteFooter() {
  const { t } = useTranslation()

  return (
    <footer className="bg-roast">
      <div className="mx-auto w-full max-w-6xl px-4 py-6 text-center text-sm text-cream sm:px-6">
        {t('footer.line')}
      </div>
    </footer>
  )
}
