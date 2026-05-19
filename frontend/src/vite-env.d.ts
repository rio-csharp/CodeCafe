/// <reference types="vite/client" />

interface CodeCafeConfig {
  apiBaseUrl?: string
}

declare global {
  interface Window {
    __CODECAFE_CONFIG__?: CodeCafeConfig
  }
}

export {}
