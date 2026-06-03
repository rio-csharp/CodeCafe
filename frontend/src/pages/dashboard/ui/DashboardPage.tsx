import logoIcon from '@/shared/assets/codecafe-icon.png'
import { useLayout } from '@/shared/model/layoutContext'
import { DashboardCards } from '@/widgets/dashboard-cards'
import { useTranslation } from 'react-i18next'

export default function DashboardPage() {
  const { user } = useLayout()
  const displayName = user?.displayName || 'there'
  const { t } = useTranslation()

  return (
    <div className="p-6 sm:p-8 lg:p-12 max-w-6xl">
      <p className="text-text-secondary text-base">{t('dashboard.greeting')}, {displayName}.</p>
      <h1 className="mt-2 text-3xl sm:text-4xl font-bold text-text-primary tracking-tight">{t('dashboard.title')}</h1>
      <p className="mt-3 text-text-secondary">{t('dashboard.subtitle')}</p>

      <DashboardCards />

      <div className="mt-8 flex items-start sm:items-center gap-4 rounded-2xl border border-border-default bg-surface-elevated p-5 sm:p-6">
        <img src={logoIcon} alt="CodeCafe" className="h-10 w-10 shrink-0" />
        <div>
          <p className="text-sm font-semibold text-text-primary">{t('dashboard.notesLive')}</p>
          <p className="text-sm text-text-secondary">{t('dashboard.notesLiveDesc')}</p>
        </div>
      </div>
    </div>
  )
}
