import { onCLS, onFCP, onINP, onLCP, onTTFB, type Metric } from 'web-vitals'
import { API_BASE_URL } from '@/shared/config'

interface VitalsPayload {
  name: string
  value: number
  rating: string
  delta?: number
  id: string
  navigationType?: string
}

const BATCH_LIMIT = 10
// Prepend API_BASE_URL so vitals reach the API origin when the frontend is
// deployed on a different origin (a relative path would silently hit the
// web host instead).
const ANALYTICS_ENDPOINT = `${API_BASE_URL}/api/analytics/vitals`

let queue: VitalsPayload[] = []
let flushTimer: ReturnType<typeof setTimeout> | null = null

function flushQueue() {
  if (queue.length === 0) return
  const batch = queue.slice()
  queue = []

  const payload = JSON.stringify({ metrics: batch })

  if (navigator.sendBeacon) {
    try {
      // A string body would be sent as text/plain; wrap in a Blob so the
      // backend can parse it as application/json.
      navigator.sendBeacon(ANALYTICS_ENDPOINT, new Blob([payload], { type: 'application/json' }))
      return
    } catch {
      // fall through to fetch
    }
  }

  try {
    fetch(ANALYTICS_ENDPOINT, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: payload,
      keepalive: true,
    }).catch(() => {
      // Silently drop analytics failures so they never crash the app.
    })
  } catch {
    // ignore
  }
}

function scheduleFlush() {
  if (flushTimer) return
  flushTimer = setTimeout(() => {
    flushTimer = null
    flushQueue()
  }, 5000)
}

function enqueue(metric: Metric) {
  queue.push({
    name: metric.name,
    value: metric.value,
    rating: metric.rating,
    delta: metric.delta,
    id: metric.id,
    navigationType: metric.navigationType,
  })

  if (queue.length >= BATCH_LIMIT) {
    if (flushTimer) {
      clearTimeout(flushTimer)
      flushTimer = null
    }
    flushQueue()
    return
  }

  scheduleFlush()
}

function sendToAnalytics(metric: Metric) {
  if (import.meta.env.DEV) {
    // eslint-disable-next-line no-console
    console.log('[Web Vitals]', metric.name, metric.value, metric.rating)
  }

  // Production: batch and send to the analytics endpoint.
  // The backend must implement POST /api/analytics/vitals.
  enqueue(metric)
}

export function reportWebVitals() {
  // Flush any queued metrics when the page is hidden or unloaded.
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      if (flushTimer) {
        clearTimeout(flushTimer)
        flushTimer = null
      }
      flushQueue()
    }
  })

  onCLS(sendToAnalytics)
  onINP(sendToAnalytics)
  onFCP(sendToAnalytics)
  onLCP(sendToAnalytics)
  onTTFB(sendToAnalytics)
}
