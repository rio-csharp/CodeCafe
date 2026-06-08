import { test as setup, expect } from '@playwright/test'

const authFile = 'e2e/.auth/user.json'

// Use a fixed e2e test account so repeated runs don't trigger rate limits.
const email = 'e2e-smoke@test.local'
const password = 'Test1234!'
const displayName = 'E2E Smoke User'

setup('authenticate', async ({ page }) => {
  // Try to log in first (most common case when the account already exists).
  await page.goto('/login')
  await page.getByTestId('login-email').fill(email)
  await page.getByTestId('login-password').fill(password)
  await page.getByTestId('login-submit').click()

  try {
    await page.waitForURL('/dashboard', { timeout: 15000 })
  } catch {
    // Login failed — the account probably doesn't exist yet. Register it.
    await page.goto('/register')
    await page.getByTestId('register-display-name').fill(displayName)
    await page.getByTestId('register-email').fill(email)
    await page.getByTestId('register-password').fill(password)
    await page.getByTestId('register-submit').click()
    await page.waitForURL('/dashboard', { timeout: 30000 })
  }

  if (!page.url().includes('/dashboard')) {
    await page.goto('/dashboard')
    await page.waitForURL('/dashboard', { timeout: 15000 })
  }

  // Verify we are authenticated using stable dashboard UI instead of page copy.
  await expect(page.getByRole('heading', { name: 'Your knowledge workspace' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Notes' })).toBeVisible()

  await page.context().storageState({ path: authFile })
})
