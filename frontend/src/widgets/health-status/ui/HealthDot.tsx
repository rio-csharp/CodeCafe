import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { fetchHealth } from '@/shared/api/health'

function HealthDot() {
  const { t } = useTranslation()
  const { data, isLoading } = useQuery({
    queryKey: ['health'],
    queryFn: fetchHealth,
    refetchInterval: 30000,
    retry: 1,
    staleTime: 25000,
  })

  const isHealthy = data?.status === 'ok'
  const dotColor = isLoading
    ? 'bg-text-tertiary'
    : isHealthy
      ? 'bg-status-success'
      : 'bg-status-error'

  const statusLabel = isLoading
    ? t('health.checking')
    : isHealthy
      ? t('health.healthy')
      : t('health.unhealthy')

  return (
    <span className="relative flex h-2 w-2" role="status" aria-label={statusLabel}>
      {!isLoading && isHealthy && (
        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-status-success-ping opacity-75" />
      )}
      <span className={`relative inline-flex h-2 w-2 rounded-full ${dotColor}`} />
    </span>
  )
}

export default HealthDot
