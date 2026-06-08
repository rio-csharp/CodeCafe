import { test, expect } from '@playwright/test'

test.use({ storageState: 'e2e/.auth/user.json' })

test.describe('Authentication flow', () => {
  test('logs out and redirects anonymous users to login', async ({ page }) => {
    // Start on a protected page as an authenticated user
    await page.goto('/notes')
    await expect(page.getByRole('heading', { name: 'Your knowledge workspace' })).toBeVisible({ timeout: 10000 })

    // Log out using the navbar button
    const logoutButton = page.getByRole('button', { name: 'Logout' })
    await expect(logoutButton).toBeVisible()
    await logoutButton.click()

    // After logout, the navbar should show the Login button
    await expect(page.getByRole('link', { name: 'Login' })).toBeVisible()

    // Direct navigation to a protected page should redirect to /login
    await page.goto('/notes')
    await page.waitForURL(/\/login/, { timeout: 10000 })
    await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible()
  })
})
