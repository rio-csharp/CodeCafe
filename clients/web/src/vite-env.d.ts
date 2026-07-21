/// <reference types="vite/client" />

interface CodeCafeConfig {
  apiBaseUrl?: string
  aiStatusEndpointPath?: string
}

declare global {
  interface Window {
    __CODECAFE_CONFIG__?: CodeCafeConfig
  }
}

export {}
