export const API_BASE_URL =
  window.__CODECAFE_CONFIG__?.apiBaseUrl ??
  import.meta.env.VITE_API_BASE_URL ??
  ''

export const AI_STATUS_ENDPOINT_PATH =
  window.__CODECAFE_CONFIG__?.aiStatusEndpointPath ??
  import.meta.env.VITE_AI_STATUS_ENDPOINT_PATH ??
  '/api/ai/status'
