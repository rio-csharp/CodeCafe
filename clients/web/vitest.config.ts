import path from 'path'
import { mergeConfig } from 'vite'
import { defineConfig } from 'vitest/config'
import viteConfig from './vite.config'

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'jsdom',
      setupFiles: [path.resolve(__dirname, './src/shared/test/setup.ts')],
      exclude: ['e2e/**', 'node_modules/**'],
    },
  }),
)
