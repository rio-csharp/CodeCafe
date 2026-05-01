import { useEffect, useState } from 'react'
import { checkBackendHealth } from '../../lib/apiClient'
import { PlatformStatus } from './PlatformStatus'

const healthCheckIntervalMs = 5_000

type HealthState = 'checking' | 'online' | 'offline'

const statusCopy: Record<HealthState, string> = {
  checking: 'Checking',
  online: 'Online',
  offline: 'Offline',
}

const statusTone: Record<HealthState, 'ready' | 'checking' | 'offline'> = {
  checking: 'checking',
  online: 'ready',
  offline: 'offline',
}

export function BackendHealthStatus() {
  const [healthState, setHealthState] = useState<HealthState>('checking')

  useEffect(() => {
    let ignoreResult = false

    async function refreshHealth() {
      try {
        const isHealthy = await checkBackendHealth()

        if (!ignoreResult) {
          setHealthState(isHealthy ? 'online' : 'offline')
        }
      } catch {
        if (!ignoreResult) {
          setHealthState('offline')
        }
      }
    }

    void refreshHealth()
    const intervalId = window.setInterval(refreshHealth, healthCheckIntervalMs)

    return () => {
      ignoreResult = true
      window.clearInterval(intervalId)
    }
  }, [])

  return (
    <PlatformStatus
      label="Backend"
      value={statusCopy[healthState]}
      tone={statusTone[healthState]}
    />
  )
}
