/* Renders favicon.svg and the LogoMark component markup to PNGs so the
   logo can be verified visually (and favicon.png regenerated small).
   Usage: node scripts/render-logo-preview.mjs  (from clients/web) */
import { chromium } from '@playwright/test'
import { readFileSync, writeFileSync } from 'node:fs'

const svg = readFileSync(new URL('../public/favicon.svg', import.meta.url), 'utf8')

// Same artwork as LogoMark.tsx, themed via currentColor
const mark = (color) => `
<svg width="128" height="128" viewBox="0 0 64 64" fill="none" style="color:${color}">
  <g stroke="#9C7C5C" stroke-width="3" stroke-linecap="round">
    <path d="M23 19c-2.8-2.5-2.8-5 0-7.5s2.8-5 0-7.5" />
    <path d="M35 19c-2.8-2.5-2.8-5 0-7.5s2.8-5 0-7.5" />
  </g>
  <path d="M14 25h32a4 4 0 0 1 4 4v14a12 12 0 0 1-12 12H22a12 12 0 0 1-12-12V29a4 4 0 0 1 4-4z" stroke="currentColor" stroke-width="4"/>
  <path d="M50 31h1.5a7.5 7.5 0 0 1 0 15H50" stroke="currentColor" stroke-width="4" stroke-linecap="round"/>
  <g stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round">
    <path d="M25.5 36l-5 5.5 5 5.5" />
    <path d="M34.5 36l5 5.5-5 5.5" />
    <path d="M31.5 35 28.5 48" />
  </g>
</svg>`

const html = `<!doctype html><html><body style="margin:0;display:flex;gap:32px;padding:32px;align-items:center;">
  <div style="background:#fff;padding:24px;border-radius:16px;">${mark('#111827')}</div>
  <div style="background:#0f0f11;padding:24px;border-radius:16px;">${mark('#f4f4f5')}</div>
  <div style="width:128px;height:128px;">${svg.replace('<svg ', '<svg width="128" height="128" ')}</div>
</body></html>`

const browser = await chromium.launch()
const page = await browser.newPage({ viewport: { width: 640, height: 220 } })
await page.setContent(html)
await page.screenshot({ path: 'logo-preview.png' })

// Regenerate a compact favicon.png (64x64) from the SVG
const fav = await browser.newPage({ viewport: { width: 64, height: 64 } })
await fav.setContent(`<!doctype html><html><body style="margin:0;">${svg.replace('<svg ', '<svg width="64" height="64" ')}</body></html>`)
await fav.screenshot({ path: '../public/favicon.png' })
await browser.close()
writeFileSync('logo-preview-done.txt', 'ok')
console.log('done')
