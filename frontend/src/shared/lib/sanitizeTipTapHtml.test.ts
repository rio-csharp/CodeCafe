import { describe, expect, it } from 'vitest'
import { sanitizeTipTapHtml } from './sanitizeTipTapHtml'

describe('sanitizeTipTapHtml', () => {
  it('removes script tags and event handler attributes', () => {
    const html = sanitizeTipTapHtml('<p onclick="alert(1)">Hello</p><script>alert(2)</script>')

    expect(html).toContain('<p>Hello</p>')
    expect(html).not.toContain('onclick')
    expect(html).not.toContain('script')
  })

  it('removes unsafe link and image URLs', () => {
    const html = sanitizeTipTapHtml(
      '<a href="javascript:alert(1)">bad</a><img src="data:image/svg+xml,<svg onload=alert(1)>">',
    )

    expect(html).toContain('<a>bad</a>')
    expect(html).not.toContain('<img')
  })

  it('keeps YouTube embed iframes but removes other iframe origins', () => {
    const html = sanitizeTipTapHtml(
      '<iframe src="https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ"></iframe><iframe src="https://evil.example/embed/dQw4w9WgXcQ"></iframe>',
    )

    expect(html).toContain('https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ')
    expect(html).toContain('sandbox=')
    expect(html).not.toContain('evil.example')
  })
})
