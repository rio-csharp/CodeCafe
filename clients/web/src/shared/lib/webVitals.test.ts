import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

vi.mock('web-vitals', () => ({
  onCLS: vi.fn(),
  onFCP: vi.fn(),
  onINP: vi.fn(),
  onLCP: vi.fn(),
  onTTFB: vi.fn(),
}))

const sendBeacon = vi.fn().mockReturnValue(true)

const fakeMetric = {
  name: 'CLS',
  value: 0.01,
  rating: 'good',
  delta: 0.01,
  id: 'metric-1',
  navigationType: 'navigate',
}

async function importFreshWebVitals(apiBaseUrl?: string) {
  vi.resetModules()
  if (apiBaseUrl !== undefined) {
    window.__CODECAFE_CONFIG__ = { apiBaseUrl }
  } else {
    delete window.__CODECAFE_CONFIG__
  }
  const webVitals = await import('./webVitals')
  const webVitalsLib = await import('web-vitals')
  return { reportWebVitals: webVitals.reportWebVitals, onCLS: vi.mocked(webVitalsLib.onCLS) }
}

// The mocked web-vitals module survives vi.resetModules(), so registrations
// accumulate; the fresh module's callback is always the latest one.
function latestSend(onCLS: ReturnType<typeof vi.fn>) {
  const calls = onCLS.mock.calls
  return calls[calls.length - 1][0] as (metric: unknown) => void
}

describe('reportWebVitals', () => {
  beforeEach(() => {
    sendBeacon.mockClear()
    Object.defineProperty(window.navigator, 'sendBeacon', {
      value: sendBeacon,
      configurable: true,
      writable: true,
    })
    vi.spyOn(console, 'log').mockImplementation(() => {})
  })

  afterEach(() => {
    vi.restoreAllMocks()
    delete window.__CODECAFE_CONFIG__
  })

  it('posts to the API origin as an application/json blob', async () => {
    const { reportWebVitals, onCLS } = await importFreshWebVitals('https://api.example.com')
    reportWebVitals()

    const send = latestSend(onCLS)
    // Fill the batch (BATCH_LIMIT = 10) to trigger an immediate flush.
    for (let i = 0; i < 10; i += 1) send(fakeMetric as never)

    expect(sendBeacon).toHaveBeenCalledTimes(1)
    const [url, body] = sendBeacon.mock.calls[0]
    expect(url).toBe('https://api.example.com/api/analytics/vitals')
    expect(body).toBeInstanceOf(Blob)
    expect((body as Blob).type).toBe('application/json')
    const parsed = JSON.parse(await (body as Blob).text())
    expect(parsed.metrics).toHaveLength(10)
    expect(parsed.metrics[0].name).toBe('CLS')
  })

  it('falls back to a same-origin relative endpoint when no API base URL is configured', async () => {
    const { reportWebVitals, onCLS } = await importFreshWebVitals()
    reportWebVitals()

    const send = latestSend(onCLS)
    for (let i = 0; i < 10; i += 1) send(fakeMetric as never)

    expect(sendBeacon).toHaveBeenCalledTimes(1)
    const [url] = sendBeacon.mock.calls[0]
    expect(url).toBe('/api/analytics/vitals')
  })
})
