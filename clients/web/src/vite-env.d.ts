/// <reference types="vite/client" />

/**
 * Runtime configuration served as `/config.js` and written by the deployment
 * (see `deploy/helm/codecafe/templates/frontend-configmap.yaml`). Every field is
 * optional because local development serves the image's empty default.
 */
interface CodeCafeConfig {
  /** Browser-facing API origin, e.g. `https://api.example.com`. Empty means same-origin. */
  apiBaseUrl?: string
}

interface Window {
  __CODECAFE_CONFIG__?: CodeCafeConfig
}
