import { test, expect } from '@playwright/test'

test.use({ storageState: 'e2e/.auth/user.json' })

test.describe('Notebook editor', () => {
  test('edits and saves a notebook page', async ({ page }) => {
    const notebookTitle = `E2E Editor ${Date.now()}`
    const paragraph = `E2E editor content ${Date.now()}`

    // Create a notebook to have a page to edit
    await page.goto('/notes')
    await page.getByTestId('new-notebook-button').click()
    await expect(page.getByRole('heading', { name: 'Create Notebook' })).toBeVisible()
    await page.getByTestId('create-notebook-title').fill(notebookTitle)
    await page.getByTestId('create-notebook-submit').click()

    // Wait for the notebook reader to load (empty notebook)
    await page.waitForURL(/\/notes\/e2e-editor/, { timeout: 30000 })
    await expect(page.getByRole('main').getByText('This notebook does not have any pages yet.')).toBeVisible()

    // Create a root page via the tree
    await page.getByRole('button', { name: 'Add folder or page' }).click()
    await page.getByRole('button', { name: 'New page' }).click()

    // Wait for the new page to be created and loaded
    await page.waitForURL(/\/notes\/e2e-editor-/, { timeout: 30000 })
    await expect(page.getByRole('button', { name: 'Edit page' })).toBeVisible()

    // Enter edit mode
    await page.getByRole('button', { name: 'Edit page' }).click()

    // Wait for the TipTap editor to become editable and enter content
    const editor = page.locator('[contenteditable="true"]').first()
    await expect(editor).toBeVisible()
    await editor.click()
    await editor.fill(paragraph)

    // Save and wait for the reader to return
    await page.getByRole('button', { name: 'Save page' }).click()

    // After saving, the edit button should reappear and the content should be visible
    await expect(page.getByRole('button', { name: 'Edit page' })).toBeVisible()
    await expect(page.getByText(paragraph)).toBeVisible()
  })
})
