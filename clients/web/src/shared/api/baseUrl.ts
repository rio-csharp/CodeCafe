/**
 * The browser-facing API origin, read from the runtime configuration the
 * deployment mounts at `/config.js`. An empty configuration leaves requests
 * same-origin, which is what local development and a bare `docker run` want.
 *
 * The split is deliberate: the SPA is served from the frontend host and calls
 * the API host, so this is the one place that knows both exist.
 */
export function getApiBaseUrl(): string {
  const configured = window.__CODECAFE_CONFIG__?.apiBaseUrl
  return typeof configured === 'string' ? configured.trim().replace(/\/+$/, '') : ''
}

/** Absolute URL for an API path. Identity while no origin is configured. */
export function apiUrl(path: string): string {
  const base = getApiBaseUrl()
  if (base === '') {
    return path
  }
  return path.startsWith('/') ? `${base}${path}` : `${base}/${path}`
}
