import { onCLS, onFCP, onINP, onLCP, onTTFB, type Metric } from 'web-vitals'

function sendToAnalytics(metric: Metric) {
  // In production, send to your analytics endpoint
  // For now, log to console in development
  if (import.meta.env.DEV) {
    // eslint-disable-next-line no-console
    console.log('[Web Vitals]', metric.name, metric.value)
  }
}

export function reportWebVitals() {
  onCLS(sendToAnalytics)
  onINP(sendToAnalytics)
  onFCP(sendToAnalytics)
  onLCP(sendToAnalytics)
  onTTFB(sendToAnalytics)
}
