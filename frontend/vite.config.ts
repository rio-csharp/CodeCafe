import path from 'path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    rolldownOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) {
            return
          }

          if (
            id.includes('lowlight')
            || id.includes('highlight.js')
          ) {
            return 'highlight-vendor'
          }

          if (
            id.includes('@tiptap')
            || id.includes('prosemirror')
          ) {
            return 'editor-vendor'
          }

          if (id.includes('react-router')) {
            return 'router-vendor'
          }

          if (id.includes('@tanstack')) {
            return 'query-vendor'
          }

          if (
            id.includes('zod')
            || id.includes('react-hook-form')
            || id.includes('@hookform')
          ) {
            return 'form-vendor'
          }

          if (id.includes('framer-motion')) {
            return 'motion-vendor'
          }

          if (
            id.includes('react')
            || id.includes('scheduler')
          ) {
            return 'react-vendor'
          }
        },
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5042',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  logLevel: 'warn',
})
