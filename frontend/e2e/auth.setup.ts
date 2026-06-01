import { test as setup, expect } from '@playwright/test'

const authFile = 'e2e/.auth/user.json'

setup('authenticate', async ({ page }) => {
  const timestamp = Date.now()
  const email = `e2e-${timestamp}@test.local`
  const password = 'Test1234!'
  const displayName = `E2E User ${timestamp}`

  // Try to register
  await page.goto('/register')
  await page.getByTestId('register-display-name').fill(displayName)
  await page.getByTestId('register-email').fill(email)
  await page.getByTestId('register-password').fill(password)

  await page.getByTestId('register-submit').click()

  // Wait for navigation (register success → dashboard or login)
  try {
    await page.waitForURL(/\/(dashboard|login)/, { timeout: 10000 })
  } catch {
    // If registration fails (e.g., email exists), try logging in
    await page.goto('/login')
    await page.getByTestId('login-email').fill(email)
    await page.getByTestId('login-password').fill(password)
    await page.getByTestId('login-submit').click()
    await page.waitForURL('/dashboard', { timeout: 10000 })
  }

  // Verify we are authenticated
  await expect(page.getByRole('heading', { name: /Welcome back|Your Workspace/ })).toBeVisible()

  await page.context().storageState({ path: authFile })
})
