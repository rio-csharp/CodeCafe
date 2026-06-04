import { useLayout } from '@/shared/model/layoutContext'
import { useTranslation } from 'react-i18next'

function CodesPage() {
  const { layout } = useLayout()
  const { t } = useTranslation()

  return (
    <div className={layout === 'sidebar' ? 'p-6 sm:p-8 lg:p-12' : 'pt-32 pb-20'}>
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <h1 className="text-3xl sm:text-4xl font-bold text-text-primary">{t('codes.title')}</h1>
        <p className="mt-4 max-w-2xl text-text-secondary">
          {t('codes.comingSoon')}
        </p>
      </div>
    </div>
  )
}

export default CodesPage
