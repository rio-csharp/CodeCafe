import { useQuery } from '@tanstack/react-query'
import { fetchHealth } from '../../lib/api/health'

function HealthDot() {
  const { data, isLoading } = useQuery({
    queryKey: ['health'],
    queryFn: fetchHealth,
    refetchInterval: 30000,
    retry: 1,
    staleTime: 25000,
  })

  const isHealthy = data?.status === 'ok'
  const dotColor = isLoading
    ? 'bg-gray-300'
    : isHealthy
      ? 'bg-green-500'
      : 'bg-red-500'

  return (
    <span className="relative flex h-2 w-2">
      {!isLoading && isHealthy && (
        <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-green-400 opacity-75" />
      )}
      <span className={`relative inline-flex h-2 w-2 rounded-full ${dotColor}`} />
    </span>
  )
}

export default HealthDot
