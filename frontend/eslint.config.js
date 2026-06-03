import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist', 'e2e/.auth', 'e2e/test-results', 'e2e/playwright-report']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
    rules: {
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/ban-ts-comment': 'warn',
      'no-console': ['warn', { allow: ['warn', 'error'] }],
    },
  },
  // pages layer: cannot import other pages
  {
    files: ['src/pages/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', {
        patterns: [
          { group: ['@/pages/*'], message: 'Pages cannot import other pages' },
        ],
      }],
    },
  },
  // widgets layer: cannot import pages or widgets
  // Note: intra-widget imports are allowed for widget composition
  {
    files: ['src/widgets/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', {
        patterns: [
          { group: ['@/pages/*'], message: 'Widgets cannot import pages' },
        ],
      }],
    },
  },
  // features layer: cannot import pages, widgets, or other features
  {
    files: ['src/features/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', {
        patterns: [
          { group: ['@/pages/*'], message: 'Features cannot import pages' },
          { group: ['@/widgets/*'], message: 'Features cannot import widgets' },
          { group: ['@/features/*'], message: 'Features cannot import other features' },
        ],
      }],
    },
  },
  // entities layer: cannot import pages, widgets, or features
  // Note: intra-entity imports are allowed for related business types
  {
    files: ['src/entities/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', {
        patterns: [
          { group: ['@/pages/*'], message: 'Entities cannot import pages' },
          { group: ['@/widgets/*'], message: 'Entities cannot import widgets' },
          { group: ['@/features/*'], message: 'Entities cannot import features' },
        ],
      }],
    },
  },
  // shared layer: cannot import any upper layers
  {
    files: ['src/shared/**/*.{ts,tsx}'],
    rules: {
      'no-restricted-imports': ['error', {
        patterns: [
          { group: ['@/app/*'], message: 'Shared cannot import upper layers' },
          { group: ['@/pages/*'], message: 'Shared cannot import upper layers' },
          { group: ['@/widgets/*'], message: 'Shared cannot import upper layers' },
          { group: ['@/features/*'], message: 'Shared cannot import upper layers' },
          { group: ['@/entities/*'], message: 'Shared cannot import upper layers' },
        ],
      }],
    },
  },
])
