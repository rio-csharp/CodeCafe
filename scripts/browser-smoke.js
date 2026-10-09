// Run with the available Playwright browser runtime's browser_run_code_unsafe
// filename option. Requires Vite at :5173 and a disposable, configured API at
// :5258. Creates a unique test account and notebook in that database.
async (page) => {
  await page.unrouteAll()
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.goto('http://localhost:5173')
  await page.evaluate(() => {
    localStorage.clear()
    localStorage.setItem('codecafe.lang', 'en')
    localStorage.setItem('codecafe.theme', 'light')
  })
  await page.goto('http://localhost:5173/register')
  const stamp = Date.now()
  await page.getByLabel('Display name', { exact: true }).fill('Smoke Reader')
  await page.getByLabel('Email', { exact: true }).fill(`smoke-${stamp}@codecafe.test`)
  await page.getByLabel('Password', { exact: true }).fill('Local-smoke-password-2026!')
  await page.getByRole('button', { name: 'Sign up', exact: true }).click()
  await page.getByRole('heading', { name: 'Your bookshelf' }).waitFor()
  await page.getByRole('button', { name: 'New notebook', exact: true }).first().click()
  const dialog = page.getByRole('dialog', { name: 'New notebook' })
  await dialog.getByLabel('Title', { exact: true }).fill(`Smoke notebook ${stamp}`)
  await dialog.getByLabel('Description (optional)').fill('A disposable end-to-end verification notebook.')
  await dialog.getByRole('combobox').selectOption('Public')
  await dialog.getByRole('button', { name: 'Create', exact: true }).click()
  await page.getByRole('button', { name: 'New page', exact: true }).click()
  await page.getByLabel('Page title', { exact: true }).fill('Persistence check')
  await page.getByRole('textbox', { name: 'Paragraph', exact: true }).fill('Saved through the browser and the real API.')
  await page.getByRole('button', { name: 'Save', exact: true }).click()
  await page.getByRole('button', { name: 'Edit', exact: true }).waitFor()
  const pageUrl = page.url()
  await page.reload()
  await page.getByText('Saved through the browser and the real API.', { exact: true }).waitFor()
  // The notebook title used to point at an unregistered root-level route.
  await page.getByRole('link', { name: `Smoke notebook ${stamp}`, exact: true }).click()
  await page.getByRole('button', { name: 'Edit', exact: true }).waitFor()
  await page.getByRole('link', { name: 'Back home', exact: true }).click()
  await page.getByRole('tab', { name: 'My notebooks', exact: true }).waitFor()
  await page.getByRole('tab', { name: 'My notebooks', exact: true }).focus()
  await page.keyboard.press('ArrowRight')
  if (await page.getByRole('tab', { name: 'Public notebooks', exact: true }).getAttribute('aria-selected') !== 'true') {
    throw new Error('Keyboard shelf navigation failed')
  }
  await page.getByRole('button', { name: /Switch theme/ }).click()
  await page.waitForFunction(() => document.documentElement.classList.contains('dark'))
  for (const width of [1440, 768, 390, 320]) {
    await page.setViewportSize({ width, height: 900 })
    if (await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)) {
      throw new Error(`Horizontal overflow at ${width}px`)
    }
  }
  await page.setViewportSize({ width: 1440, height: 1000 })
  await page.getByRole('button', { name: 'Smoke Reader', exact: true }).click()
  await page.getByRole('menuitem', { name: 'Log out', exact: true }).click()
  await page.getByRole('link', { name: 'Log in', exact: true }).waitFor()
  await page.goto(pageUrl)
  await page.getByText('Saved through the browser and the real API.', { exact: true }).waitFor()
  if (await page.getByRole('button', { name: 'Edit', exact: true }).count()) {
    throw new Error('Anonymous reader can see editing controls')
  }
  return { passed: true, checks: ['register', 'create public notebook', 'create page', 'save', 'reload persistence', 'notebook root link', 'keyboard shelf navigation', 'dark theme', 'responsive widths', 'logout', 'anonymous reading'] }
}
