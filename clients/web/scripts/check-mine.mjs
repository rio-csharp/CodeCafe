import { chromium } from '@playwright/test'
const browser = await chromium.launch()
const ctx = await browser.newContext({ storageState: 'e2e/.auth/user.json' })
const page = await ctx.newPage()
for (const url of ['http://localhost:5042/api/notes/mine?limit=50', 'http://localhost:5042/api/notes/public?limit=50']) {
  const resp = await page.request.get(url)
  const body = await resp.text()
  console.log(url, '->', resp.status(), body.slice(0, 400))
}
await browser.close()
