import { test, expect } from '@playwright/test'

test.use({ storageState: 'e2e/.auth/user.json' })

test.describe('Notebook lifecycle', () => {
  test('create and delete a notebook', async ({ page }) => {
    const notebookTitle = `E2E Notebook ${Date.now()}`

    // Navigate to Notes page
    await page.goto('/notes')
    await expect(page.getByTestId('new-notebook-button')).toBeVisible()

    // Click "New Notebook"
    await page.getByTestId('new-notebook-button').click()
    await expect(page.getByRole('heading', { name: 'Create Notebook' })).toBeVisible()

    // Fill form
    await page.getByTestId('create-notebook-title').fill(notebookTitle)
    await page.getByTestId('create-notebook-description').fill('Created by E2E test')
    await page.getByTestId('create-notebook-submit').click()

    // Should redirect to notebook page
    await page.waitForURL(/\/notes\/e2e-/, { timeout: 10000 })

    // Navigate back to Notes list
    await page.goto('/notes')
    await expect(page.getByText(notebookTitle)).toBeVisible()

    // Open card menu and delete
    const card = page.getByTestId('notebook-card').filter({ hasText: notebookTitle })
    await card.getByTestId('notebook-menu-button').click()

    await card.getByTestId('notebook-delete-button').click()

    // Deletion is confirmed through the in-app dialog
    await page.getByRole('dialog').getByRole('button', { name: /confirm|确认/i }).click()

    // Wait for delete API response
    await page.waitForResponse(
      (resp) => resp.url().includes('/api/notes') && resp.request().method() === 'DELETE',
      { timeout: 10000 },
    )

    // Notebook should disappear
    await expect(page.getByText(notebookTitle)).not.toBeVisible()
  })
})
