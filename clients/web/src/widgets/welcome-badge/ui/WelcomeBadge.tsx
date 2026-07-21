import HealthDot from '@/widgets/health-status'
import { useTranslation } from 'react-i18next'

function WelcomeBadge() {
  const { t } = useTranslation()
  return (
    <span className="inline-flex items-center gap-2 rounded-full border border-border-default bg-surface px-4 py-1.5 text-xs font-medium text-text-secondary">
      <HealthDot />
      {t('app.name')} — {t('nav.notes')} + MCP
    </span>
  )
}

export default WelcomeBadge
