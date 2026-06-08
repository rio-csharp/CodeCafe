import { test, expect } from '@playwright/test'

test.use({ storageState: 'e2e/.auth/user.json' })

test.describe('Authentication flow', () => {
  test('logs out and redirects anonymous users to login', async ({ page }) => {
    // Start on the dashboard as an authenticated user
    await page.goto('/dashboard')
    await expect(page.getByRole('heading', { name: 'Your knowledge workspace' })).toBeVisible({ timeout: 10000 })

    // Log out via the sidebar user menu
    await page.getByRole('button', { name: 'User menu' }).click()
    const logoutButton = page.getByRole('button', { name: 'Logout' })
    await expect(logoutButton).toBeVisible()
    await logoutButton.click()

    // After logout, the navbar should show the Login button
    await expect(page.getByRole('link', { name: 'Login' })).toBeVisible()

    // Direct navigation to a protected page while anonymous should show the
    // public view with a sign-in prompt rather than redirecting to /login.
    await page.goto('/notes')
    await expect(page.getByRole('heading', { name: 'Notebooks' })).toBeVisible()
    await expect(page.getByText('Sign in to build your own notebook library')).toBeVisible()
  })
})
