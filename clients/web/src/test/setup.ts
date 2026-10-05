import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'
// Side effect: initialises i18next for every test file.
import '@/shared/i18n'

afterEach(() => {
  cleanup()
})
